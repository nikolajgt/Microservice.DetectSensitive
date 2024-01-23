using Microservice.JobScheduler.Infrastructure;

namespace Microservice;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly JobSchedulerService _jobSchedulerService;

    public Worker(
        ILogger<Worker> logger,
        JobSchedulerService service)
    {
        _logger = logger;
        _jobSchedulerService = service;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                var jobs = _jobSchedulerService.GetAciveJobs();
                foreach (var job in jobs)
                {
                    _logger.LogInformation("Job history id: {jobhistoryid} is running", job.Id);
                }
            }
            await Task.Delay(1000, stoppingToken);
        }
    }
}
