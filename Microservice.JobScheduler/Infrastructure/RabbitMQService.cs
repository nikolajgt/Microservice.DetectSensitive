using MessagePack;
using Microservice.Domain;
using Microservice.Domain.Base.Enums;
using Microservice.JobScheduler.Application.Validation;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.IO;
using System.Threading.Channels;


namespace Microservice.JobScheduler.Infrastructure;

internal class RabbitMQService
{
    private readonly ILogger<RabbitMQService> _logger;
    private readonly JobSchedulerService _jobSchedulerService;
    private readonly IConnection _connection;
    public string exchange { get; } = "DataHarvest";
    public string getRequestQueueName { get; } = "DataHarvest_JobSchedulerGetRequestQueue";
    public string getResponseQueueName { get; } = "DataHarvest_JobSchedulerGetResponseQueue";
    public string jobFinishedResponseQueueName { get; } = "DataHarvest_JobSchedulerGetResponseQueue";

    public RabbitMQService(
        ILogger<RabbitMQService> logger,
        JobSchedulerService jobSchedulerService,
        IHostApplicationLifetime lifetime)
    {
        _logger = logger;
        _jobSchedulerService = jobSchedulerService;
        var connectionFactory = new ConnectionFactory
        {
            HostName = "localhost", // RabbitMQ server host
            Port = 5672,            // RabbitMQ server port
            UserName = "guest",     // RabbitMQ username
            Password = "guest"      // RabbitMQ password
        };
        _connection = connectionFactory.CreateConnection();
        var _requestChannel = _connection.CreateModel();
        var _responseChannel = _connection.CreateModel();

        // Gets ignored if already exists
        _requestChannel.ExchangeDeclare(exchange: exchange, type: ExchangeType.Direct);
        // Queues to get messages out to harvester
        _requestChannel.QueueDeclare(queue: getRequestQueueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
        _responseChannel.QueueDeclare(queue: getResponseQueueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
    }

    public IModel GetConnection()
    {
        return _connection.CreateModel();
    }

}
