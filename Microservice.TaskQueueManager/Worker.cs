using Microservice.TaskManagQueueManager.Application.IServices;
using Microservice.TaskManagQueueManager.Infrastructure;
using Microservice.TaskManagQueueManager.Infrastructure.Queues;

namespace Microservice.TaskDistributor;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly QueueService _queueService;
    private readonly IJobRequestResponderService _service;

    public Worker(
        ILogger<Worker> logger,
        QueueService queueService,
        IJobRequestResponderService service)
    {
        _logger = logger;
        _queueService = queueService;
        _service = service;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if(_queueService.IsTimeForNextJob(out var harvesterType))
            {
                // use the hosted service here and call the request job function on the service
                await _service.ReqeustJobAsync(harvesterType);
            }
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Task queue manager Worker running at: {time}", DateTimeOffset.Now);
            }
            await Task.Delay(5000, stoppingToken);
        }
    }
}
