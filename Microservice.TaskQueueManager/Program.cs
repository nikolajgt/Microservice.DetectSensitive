using Microservice.Domain;
using Microservice.TaskDistributor;
using Microservice.TaskManagQueueManager.Application.IServices;
using Microservice.TaskManagQueueManager.Infrastructure;
using Microservice.TaskManagQueueManager.Infrastructure.Queues;
using Microservice.TaskQueueManage.Infrastructure;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.EventLog;
using Serilog;

await new HostBuilder()
    .UseEnvironment(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development")
    .ConfigureAppConfiguration(ConfigureAppConfiguration)
    .ConfigureServices(ConfigureServices)
    .Build()
    .RunAsync();

static void ConfigureAppConfiguration(
    HostBuilderContext context,
    IConfigurationBuilder config)
{
    // Clearing all default configuration providers
    config.Sources.Clear();
    // Adding configuration from appsettings.json
    config
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true,
            reloadOnChange: true);
}

static void ConfigureServices(
    HostBuilderContext context,
    IServiceCollection services)
{
    Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(context.Configuration)
    .WriteTo.Console()
    .CreateLogger();

    services
        .AddLogging(configure =>
        {
            configure.ClearProviders();
            configure.AddSerilog(dispose: true);
        });

    services.AddSingleton<RabbitMQService>();
    services.AddSingleton<QueueService>();

    
    services.AddSingleton<IJobRequestResponderService, JobRequestResponderService>();
    services.AddHostedService(provider => provider.GetService<IJobRequestResponderService>() as JobRequestResponderService );
    
    services.AddHostedService<Worker>();
}


