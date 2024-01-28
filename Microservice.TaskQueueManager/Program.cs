using Microservice.TaskDistributor;
using Microservice.TaskManagQueueManager.Infrastructure;
using Microservice.TaskQueueManage.Infrastructure;
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
    .CreateLogger();

    services
        .AddLogging(configure =>
        {
            configure.ClearProviders();
            configure.AddSerilog(dispose: true);
        });

    services.AddSingleton<RabbitMQService>();
    services.AddSingleton<QueueService>();
    services.AddSingleton<GetJobQueueListenerService>();
    services.AddHostedService<Worker>();

    services.AddHostedService<Worker>();
}


