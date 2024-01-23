using Microservice.TaskDistributor;
using Microservice.TaskManagQueueManager.Infrastructure;
using Microservice.TaskQueueManage.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<RabbitMQService>();
builder.Services.AddSingleton<QueueService>();
builder.Services.AddSingleton<GetJobQueueListenerService>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
