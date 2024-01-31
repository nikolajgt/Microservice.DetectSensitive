using Microservice.Domain.Base.Enums;
using Microservice.Domain.Models.TaskQueueManager;
using Microservice.TaskManagQueueManager.Models;
using Microservice.TaskQueueManage.Infrastructure;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Microservice.TaskManagQueueManager.Infrastructure;

public class QueueService
{
    private readonly ILogger<QueueService> _logger;
    // way to tie jobhistoryid to the taskwork that contains tasks
    // tasks from each of these will get removed when starting so at some point
    // no tasks is in the TaskWorkJob and it is completed by checking max created count
    private readonly ConcurrentDictionary<Guid, TaskWorkJob> _activeTaskWorkJobs;

    // way to see the tasks that is currently getting processed by harvesters
    // then we can Queue up if failed less then 4 or if over report as partial failed
    private readonly HashSet<TaskWorkRequest> _activeTasks;

    // Queue to get next job, only holds limited jobs before requesting new
    private readonly Queue<TaskWorkJob> _taskWorkJobsQueue;

    // Retry Queue for tasks so they can run later, but have higher priorty than whats in _activeTaskWorkJobs 
    // when they ready, so we can get done with the job
    private readonly Queue<TaskWorkRequest> _retryQueue;

    private readonly QueueConfig _queueConfig;

    // Multiplyer for _taskWorkJobsQueue to not overlaod with to many
    // calculated dyncamicly with heartbeats count from harvesters and
    // number of tasks in all of _activeTaskWorkJobs
    private const int tasksBufferMultiplyer = 2;
    public QueueService(
        ILogger<QueueService> logger)
    { 
        _logger = logger;
        _queueConfig = new QueueConfig(HarvesterType.FileSystem, string.Empty, 3);
        _activeTaskWorkJobs = new ConcurrentDictionary<Guid, TaskWorkJob>();
        _activeTasks = new HashSet<TaskWorkRequest>();
        _taskWorkJobsQueue = new Queue<TaskWorkJob>();
        _retryQueue = new Queue<TaskWorkRequest>();
    }

    private async Task StartNextTask()
    {
        // Check if there are any tasks left in the active jobs
        foreach (var taskWorkJob in _activeTaskWorkJobs.Values)
        {
            if(taskWorkJob.Tasks.TryDequeue(out var task))
            {
               // _rabbitMQ.SendData(task);
                _activeTasks.Add(task);
            }
            else
            {
                await CleanupTasks();
            }
        }

        // If no remaining tasks in active jobs, dequeue a new job
        if (_taskWorkJobsQueue.TryDequeue(out var taskWorkJobNew))
        {
            if(taskWorkJobNew.Tasks.TryDequeue(out var task))
            {
                _activeTaskWorkJobs[taskWorkJobNew.JobHistoryId] = taskWorkJobNew;
                _activeTasks.Add(task);
            }
            else
            {
                _logger.LogError("TaskWorkJob contained 0 tasks inside from job history id {jobhistoryid}", taskWorkJobNew.JobHistoryId);
            }
        }
        else
        {
            // call for new
        }
    }

    public bool IsTimeForNextJob(out HarvesterType harvesterType)
    {
        var GetActiveHarvesterCount = 1;
        if (GetActiveHarvesterCount * tasksBufferMultiplyer > _taskWorkJobsQueue.Count)
        {
            harvesterType = HarvesterType.FileSystem;
            return true;
        }

        harvesterType = HarvesterType.None;
        return false;
    }

    // function to clean all of the jobs
    private async Task CleanupTasks()
    {
        var taskWorkJobGroup = _activeTaskWorkJobs
            .Where(x => x.Value.Tasks.Count > 0)
            .Where(x => !x.Value.Tasks.Any(task => _activeTasks.Contains(task)))
            .Select(x => x.Value)
            .ToList();

        foreach (var taskWorkJob in taskWorkJobGroup)
        {
            _activeTaskWorkJobs.Remove(taskWorkJob.JobHistoryId, out var _);
            // call jobscheduler queue
        }
    }

    private async Task TaskIsFinishedSuccessfully(TaskWorkResponse taskResponse)
    {
        try
        {
            if (_activeTaskWorkJobs.TryGetValue(taskResponse.JobHistoryId, out var taskWorkJob))
            {
                if (taskWorkJob.Tasks.Count <= 0)
                {
                   // Call update jobscheduler
                    _activeTaskWorkJobs.Remove(taskResponse.JobHistoryId, out var _);
                }
            }
            else
            {
                _logger.LogError("Job history didnt exist with jobHistoryId {jobhistoryId}", taskResponse.JobHistoryId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Task got removed from active tasks before it back from harvester");
        }
        finally
        {
            await StartNextTask();
        }
    }


    private async Task TaskFailedWithError(TaskWorkResponse taskResponse)
    {
        try
        {
            if (_activeTaskWorkJobs.TryGetValue(taskResponse.JobHistoryId, out var taskWorkJob))
            {
                var task = _activeTasks.FirstOrDefault(x => x.Id ==  taskResponse.TaskId);
                if (task == null)
                {
                    _logger.LogError("Task that failed returned but didnt exist in active tasks");
                }

                if (task!.RetryCount > _queueConfig.MaxNumberOfTaskRetrys)
                {
                    taskWorkJob.IsFailed = true;
                    _activeTasks.Remove(task);
                    _logger.LogError("Task max retry reached, do somethign about it");
                }
                else
                {
                    _logger.LogInformation("Requeuing failed task with jobhistory id: {jobHistoryId}", task.JobHistoryId);
                    task.RetryCount++;
                    _retryQueue.Enqueue(task);
                }

                if (taskWorkJob.Tasks.Count <= 0)
                {
                    await CleanupTasks();
                }
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing task failure");
        }
        finally
        {
            await StartNextTask();
        }
    }
}
