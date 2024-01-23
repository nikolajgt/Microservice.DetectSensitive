using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microservice.JobScheduler;

public class JobSchedulerConfig
{
    public JobSchedulerConfig(
        IConfiguration configuration,
        ILogger<JobSchedulerConfig> logger)
    {
        ConnectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        logger.LogInformation("JobScheduler database conncetion string: {ConnectionString} ", ConnectionString);
    }

    public string ConnectionString { get; }
}
