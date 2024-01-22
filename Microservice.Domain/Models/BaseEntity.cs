using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microservice.Domain.Models;

public class BaseEntity
{
    public virtual Guid Id { get; set; } = Guid.NewGuid();
}
