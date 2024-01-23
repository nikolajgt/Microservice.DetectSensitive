using Microservice.Domain.Enums;


namespace Microservice.Domain.Models.Scheduler;

public class JobHistory : BaseEntity
{
    public JobStatus Status { get; set; } = JobStatus.Processing;
    public DateTime Started { get; set; } = DateTime.UtcNow;
    public DateTime? Finished { get; set; }
    public bool Failed { get; set; } = false;
    public Guid JobId { get; set; }
    public Job Job { get; set; }

    public JobHistory(Job job)
    {
        Job = job;
        JobId = job.Id;
    }
}
