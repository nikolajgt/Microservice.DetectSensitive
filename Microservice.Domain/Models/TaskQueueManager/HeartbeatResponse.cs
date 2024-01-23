using MessagePack;
using Microservice.Domain.Base.Enums;

namespace Microservice.Domain.Models.TaskQueueManager;
[MessagePackObject]
public class HeartbeatResponse
{
    [Key(0)]
    public Guid HarvesterId { get; set; }
    [Key(1)]
    public HarvesterType HarvesterType { get; set; }
    [Key(2)]
    public bool IsAlive { get; set; }
}
