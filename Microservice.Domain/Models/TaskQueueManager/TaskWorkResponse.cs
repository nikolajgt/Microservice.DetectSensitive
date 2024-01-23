using MessagePack;

namespace Microservice.Domain.Models.TaskQueueManager;

[MessagePackObject]
public class TaskWorkResponse
{
    [Key(0)]
    public Guid JobHistoryId { get; set; }
    [Key(1)]
    public Guid TaskId { get; set; }
    [Key(2)]
    public bool Failed { get; set; }
}
