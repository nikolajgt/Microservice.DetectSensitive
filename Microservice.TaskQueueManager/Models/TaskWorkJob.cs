using Microservice.Domain.Enums;
using Microservice.Domain.Models;
using Microservice.Domain.Models.TaskQueueManager;

namespace Microservice.TaskManagQueueManager.Models;


public class TaskWorkJob
{
    public Guid JobHistoryId { get; set; }
    public string SourceAddress { get; set; }
    public JobType JobType { get; set; }
    public Queue<TaskWorkRequest> Tasks { get; set; }

    public int MaxCreated { get; }
    // should later become a enum fx completed, minor exceptions, partial failed, failed 
    public bool IsFailed { get; set; } = false;


    public TaskWorkJob(
        Guid jobHistoryId,
        string sourceAddress,
        JobType jobType,
        Queue<TaskWorkRequest> tasks)
    {
        JobHistoryId = jobHistoryId;
        SourceAddress = sourceAddress;
        JobType = jobType;
        Tasks = tasks;
        MaxCreated = tasks.Count;
    }
}
