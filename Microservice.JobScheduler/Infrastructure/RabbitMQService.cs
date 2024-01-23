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

public class RabbitMQService
{
    private readonly ILogger<RabbitMQService> _logger;
    private readonly IConnection _connection;
    public string exchange { get; } = "DataHarvest";
    public string QueueRequestReadyJob { get; } = "DataHarvest_RequestReadyJob";
    public string QueueRespondReadyJob { get; } = "DataHarvest_RespondReadyJob";
    public string QueueRespondFinishedJob { get; } = "DataHarvest_RespondFinishedJob";

    public RabbitMQService(
        ILogger<RabbitMQService> logger)
    {
        _logger = logger;
        var connectionFactory = new ConnectionFactory
        {
            HostName = "rabbitmq", // RabbitMQ server host
            Port = 5672,            // RabbitMQ server port

        };
        _connection = connectionFactory.CreateConnection();
        var _requestChannel = _connection.CreateModel();
        var _responseChannel = _connection.CreateModel();

        // Gets ignored if already exists
        _requestChannel.ExchangeDeclare(exchange: exchange, type: ExchangeType.Direct);
        // Queues to get messages out to harvester
     
    }


    public IModel GetConnection()
    {
        return _connection.CreateModel();
    }

}
