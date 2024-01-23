using MessagePack;
using Microservice.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microservice.Domain.Models.TaskQueueManager;

[MessagePackObject]
public class TaskWorkRequest : BaseEntity
{
    [Key(0)]
    public override Guid Id { get; set; } = Guid.NewGuid();
    [Key(1)]
    public IEnumerable<string>? Addresses { get; set; }
    [Key(2)]
    public Guid JobHistoryId { get; set; }
    [Key(3)]
    public JobType JobType { get; set; }


    public int RetryCount { get; set; } = 0;
}

