using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microservice.Domain;

public class JobResponse
{
    public Guid JobHistoryId { get; set; }
    public string Address { get; set; }
}
