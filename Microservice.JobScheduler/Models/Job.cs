using Microservice.Domain.Enums;
using Microservice.Domain.Models.Scheduler;

namespace Microservice.Domain.Models;

public class Job : BaseEntity
{

    public string JobName { get; set; }
    public JobType JobType { get; set; }
    public JobStatus JobStatus { get; set; }
    public Guid SourceId { get; set; }

    public Source Source { get; set; }
    public string CronSchedule { get; set; }
    public bool Force { get; set; }
    public DateTime LastRun { get; set; }
    public IEnumerable<JobHistory> History { get; set; }



    public Job(
        string jobName,
        JobType jobType,
        JobStatus jobStatus,
        Source source,
        string cronSchedule,
        bool force,
        DateTime lastRunDate)
    {
        JobName = jobName;
        JobType = jobType;
        JobStatus = jobStatus;
        Source = source;
        CronSchedule = cronSchedule;
        Force = force;
        LastRun = lastRunDate;
        History = new List<JobHistory>();
    }

    public Job(
        Guid id,
        string jobName,
        JobType jobType,
        JobStatus jobStatus,
        Source source,
        string cronSchedule,
        bool force,
        DateTime lastRunDate)
    {
        Id = id;
        JobName = jobName;
        JobType = jobType;
        JobStatus = jobStatus;
        Source = source;
        CronSchedule = cronSchedule;
        Force = force;
        LastRun = lastRunDate;
        History = new List<JobHistory>();
    }
}