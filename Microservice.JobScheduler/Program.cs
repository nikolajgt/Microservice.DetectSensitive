using Microservice;
using Microservice.JobScheduler.Infrastructure;
using Microservice.JobScheduler.Infrastructure.Database;
using Microservice.JobScheduler.Infrastructure.QueueListeners;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddTransient<DatabaseService>();
builder.Services.AddSingleton<JobSchedulerService>();
builder.Services.AddSingleton<RabbitMQService>();

builder.Services.AddSingleton<GetJobQueueService>();
builder.Services.AddSingleton<JobFinishedQueueService>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
