using MessagePack;
using Microservice.Domain;
using Microservice.Domain.Base.Enums;
using Microservice.JobScheduler.Application.Validation;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Channels;


namespace Microservice.JobScheduler.Infrastructure;

internal class RabbitMQService
{
    private readonly ILogger<RabbitMQService> _logger;
    private readonly JobSchedulerService _jobSchedulerService;
    private readonly ConnectionFactory _connectionFactory;
    public string exchange { get; } = "DataHarvest";
    public string jobSchedulerRequestQueueName { get; } = "DataHarvest_JobSchedulerRequestQueue";
    public string jobSchedulerResponseQueueName { get; } = "DataHarvest_JobSchedulerRequestResponseQueue";
    public RabbitMQService(
        ILogger<RabbitMQService> logger,
        JobSchedulerService jobSchedulerService,
        IHostApplicationLifetime lifetime)
    {
        _logger = logger;
        _jobSchedulerService = jobSchedulerService;
        _connectionFactory = new ConnectionFactory
        {
            HostName = "localhost", // RabbitMQ server host
            Port = 5672,            // RabbitMQ server port
            UserName = "guest",     // RabbitMQ username
            Password = "guest"      // RabbitMQ password
        };

        var _requestChannel = _connectionFactory.CreateConnection().CreateModel();
        var _responseChannel = _connectionFactory.CreateConnection().CreateModel();

        // Gets ignored if already exists
        _requestChannel.ExchangeDeclare(exchange: exchange, type: ExchangeType.Direct);
        // Queues to get messages out to harvester
        _requestChannel.QueueDeclare(queue: jobSchedulerRequestQueueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
        _responseChannel.QueueDeclare(queue: jobSchedulerResponseQueueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
    }

    public IModel GetConnection()
    {
        return _connectionFactory.CreateConnection().CreateModel();
    }

}
