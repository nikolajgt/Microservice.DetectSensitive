using Microservice.TaskManagQueueManager.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microservice.TaskManagQueueManager.Infrastructure;

internal class QueueService
{
    private readonly ILogger<QueueService> _logger;
    private readonly ConcurrentDictionary<Guid, List<TaskWorkJob>> _activeTaskWorkJobs;

    public QueueService(ILogger<QueueService> logger)
    { _logger = logger; }


}
