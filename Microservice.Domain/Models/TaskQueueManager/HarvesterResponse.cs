using MessagePack;


namespace Microservice.Domain.Models.TaskQueueManager;


public class HarvesterResponse
{
    [Key(0)]
    public HeartbeatResponse? HeartbeatResponse { get; set; }
    [Key(1)]
    public TaskWorkResponse? TaskWorkResponse { get; set; }
}
