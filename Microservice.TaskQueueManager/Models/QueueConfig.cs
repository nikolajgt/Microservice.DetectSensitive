using Microservice.Domain.Base.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microservice.TaskManagQueueManager.Models;

public class QueueConfig
{
    public HarvesterType HarvesterType { get; }
    public string QueueName { get; }
    public int MaxNumberOfTaskRetrys { get; }

    public QueueConfig(HarvesterType harvesterType, string queueName, int maxNumberOfTaskRetrys)
    {
        HarvesterType = harvesterType;
        QueueName = queueName;
        MaxNumberOfTaskRetrys = maxNumberOfTaskRetrys;
    }
}
