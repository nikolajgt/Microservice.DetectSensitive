using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microservice.Domain;
[MessagePackObject]
public class JobResponse
{
    [Key(0)]
    public Guid JobHistoryId { get; set; }
    [Key(1)]
    public string Address { get; set; }
}
