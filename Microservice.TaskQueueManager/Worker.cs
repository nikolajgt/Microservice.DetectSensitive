using Microservice.TaskManagQueueManager.Infrastructure;

namespace Microservice.TaskDistributor;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly QueueService _queueService;

    public Worker(
        ILogger<Worker> logger,
        QueueService queueService)
    {
        _logger = logger;
        _queueService = queueService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if(await _queueService.IsTimeForNextJob())
            {
            }
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Task queue manager Worker running at: {time}", DateTimeOffset.Now);
            }
            await Task.Delay(5000, stoppingToken);
        }
    }
}
