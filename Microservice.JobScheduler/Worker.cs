using Microservice.JobScheduler.Infrastructure;
using Microservice.JobScheduler.Infrastructure.QueueListeners;

namespace Microservice;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly JobSchedulerService _jobSchedulerService;

    public Worker(
        ILogger<Worker> logger,
        JobSchedulerService jobSchedulerService,
         GetJobQueueListenerService listner)
    {
        _jobSchedulerService = jobSchedulerService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("JobScheduler Worker running at: {time}", DateTimeOffset.Now);

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
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error in Worker Jobscheduler");
            }
        }
    }
}
