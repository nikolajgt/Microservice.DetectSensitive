using Microservice.Domain.Base.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microservice.TaskManagQueueManager.Application.IServices;

public interface IJobRequestResponderService
{

    Task ReqeustJobAsync(HarvesterType harvesterType);
}
