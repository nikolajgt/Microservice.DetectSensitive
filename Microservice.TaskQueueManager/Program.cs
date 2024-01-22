using Microservice.TaskDistributor;
using Microservice.TaskQueueManage.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<RabbitMQService>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
