using Microservice;
using Microservice.JobScheduler;

using Microservice.JobScheduler.Infrastructure;
using Microservice.JobScheduler.Infrastructure.Database;
using Microservice.JobScheduler.Infrastructure.QueueListeners;
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
            reloadOnChange: true)
        .AddEnvironmentVariables();
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


    services.AddSingleton<JobSchedulerConfig>();
    services.AddSingleton<RabbitMQService>();
    services.AddTransient<DatabaseService>();

    services.AddSingleton<JobSchedulerService>();

    services.AddSingleton<JobRequestResponderService>();
    services.AddHostedService(provider => provider.GetService<JobRequestResponderService>());

    services.AddHostedService<Worker>();
}



