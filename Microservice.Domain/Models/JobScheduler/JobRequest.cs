using Microservice.Domain.Base.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microservice.Domain;

public class JobRequest
{
    public HarvesterType HarvesterType { get; set; }
}

