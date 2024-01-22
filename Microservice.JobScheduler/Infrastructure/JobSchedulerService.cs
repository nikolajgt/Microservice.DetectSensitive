using Microservice.Domain.Base.Enums;
using Microservice.Domain.Enums;
using Microservice.Domain.Models.Scheduler;
using Microservice.Domain.Models;
using Microservice.JobScheduler.Infrastructure.Database;
using Microservice.JobScheduler.Application.Extensions;

namespace Microservice.JobScheduler.Infrastructure;

public class JobSchedulerService
{
    private readonly ILogger<JobSchedulerService> _logger;

    private readonly HashSet<JobHistory> _activeJobs;
    private readonly HashSet<Job> _jobQueue;
    private readonly HashSet<Job> _jobCache;
    private readonly DatabaseService _dbService;

    public JobSchedulerService(
        IServiceProvider provider,
        IHostApplicationLifetime lifetime,
        DatabaseService databaseService,
        ILogger<JobSchedulerService> logger)
    {
        _jobQueue = new HashSet<Job>();
        _jobCache = new HashSet<Job>();
        _activeJobs = new HashSet<JobHistory>();
        _dbService = databaseService;
        _logger = logger;

        Task.Run(async () =>
        {
            await using var scope = provider.CreateAsyncScope();
            await SyncJobsDatabase(scope.ServiceProvider, lifetime.ApplicationStopping);
        });
    }

    public async Task<JobHistory> DequeueNextReadyJobAsync(CancellationToken cancellationToken)
    {
        var nextJob = _jobQueue
            .OrderByDescending(j => j.Force)
            .ThenBy(j => j.LastRun)
            .Select(j =>
            {
                j.LastRun = DateTime.UtcNow;
                j.JobStatus = JobStatus.Processing;
                j.Force = false;
                return new JobHistory(j);
            })
            .FirstOrDefault();

        if (nextJob is null)
        {
            _logger.LogInformation("JobScheduler do no thave any jobs ready");
            return null;
        }

        _activeJobs.Add(nextJob);
        await _dbService.StartJobAsync(nextJob, cancellationToken);
        _logger.LogInformation("JobScheduler have started job [{JobId}] [{jobType}]", nextJob.JobId, nextJob.Job.JobType);
        return nextJob;
    }

    public async Task<JobHistory> DequeueNextReadyJobAsync(HarvesterType harvesterType, CancellationToken cancellationToken)
    {
        try
        {
            var nextJob = _jobQueue
           .Where(x => x.JobType.IsExchangeOrFileSystem() == harvesterType)
           .OrderByDescending(j => j.Force)
           .ThenBy(j => j.LastRun)
           .Select(j =>
           {
               j.LastRun = DateTime.UtcNow;
               j.JobStatus = JobStatus.Processing;
               j.Force = false;
               return new JobHistory(j);
           })
           .FirstOrDefault();

            if (nextJob is null)
            {
                _logger.LogInformation("JobScheduler do no thave any jobs ready");
            }

            await _dbService.StartJobAsync(nextJob!, cancellationToken);
            _logger.LogInformation("JobScheduler have started job [{JobId}] [{jobType}]", nextJob.JobId, nextJob.Job.JobType);
            return nextJob;
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Error at DequeueNextReadyJobAsync in JobSchedulerService");
            return null;
        }
    }


    public void CheckReadyJobs()
    {
        foreach (var job in _jobCache)
        {
            if (job.CronSchedule?.IsDue(job.LastRun) ?? false)
            {
                if (_jobQueue.Any(x => x.Id == job.Id)) continue;
                _jobQueue.Add(job);
            }
        }
    }

    public async Task UpdateJobHistory(JobHistory jobHistory, CancellationToken cancellationToken)
    {
        await _dbService.FinishJobAsync(jobHistory, cancellationToken);
    }

    public async Task UpdateJobHistory(Guid jobHistoryId, CancellationToken cancellationToken)
    {
        try
        {
            var jobHistory = _activeJobs.FirstOrDefault(x => x.Id == jobHistoryId);
            if (jobHistory is null)
            {
                _logger.LogError("Job has been removed from Jobscheduler but got new UpdateJobHistoryid call with Jobhistory id {Jobhistoryid}", jobHistoryId);
            }

            _activeJobs.Remove(jobHistory!);
            await _dbService.FinishJobAsync(jobHistory!, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error at UpdateJobHistory in  JobSchedulerservice");
        }
    }

    private async Task SyncJobsDatabase(IServiceProvider sp, CancellationToken cancellationToken)
    {
        var dbService = sp.GetRequiredService<DatabaseService>();
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var jobs = await dbService.GetJobsAsync(cancellationToken);
                foreach (var job in _jobCache)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (jobs.Any(x => x.Id == job.Id)) continue;
                    _jobCache.Add(job);
                }

                await Task.Delay(TimeSpan.FromMinutes(10), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Handle cancellation
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during SyncJobsDatabase.");
                // Handle other exceptions, maybe wait before retrying
            }
        }
    }

}
