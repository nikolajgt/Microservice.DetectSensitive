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
    public IModel? GetRequestReadyJobQueue { get; }
    public IModel? GetRespondReadyJobQueue { get; }
    public IModel? GetFinishedReadyJobQueue { get; }

    public string exchange { get; } = "DataHarvest";
    public string RequestReadyJobName { get; } = "DataHarvest_RequestReadyJob";
    public string RespondReadyJobName { get; } = "DataHarvest_RespondReadyJob";
    public string RespondFinishedJobName { get; } = "DataHarvest_RespondFinishedJob";

    public RabbitMQService(
        ILogger<RabbitMQService> logger,
        IHostApplicationLifetime lifetime)
    {
        _logger = logger;
        var connectionFactory = new ConnectionFactory
        {
            HostName = "rabbitmq", // RabbitMQ server host

        };
        int retryCount = 5;
        while (retryCount > 0)
        {
            try
            {
                _connection = connectionFactory.CreateConnection();
                break; // Success, exit the loop
            }
            catch
            {
                retryCount--;
                Console.WriteLine("Connection failed, retrying...");
                Task.Delay(5000); // Wait for 5 seconds before retrying

            }
        }
        GetRequestReadyJobQueue = _connection.CreateModel();
        GetRespondReadyJobQueue = _connection.CreateModel();
        GetFinishedReadyJobQueue = _connection.CreateModel();

        GetRequestReadyJobQueue.ExchangeDeclare(exchange: exchange, type: ExchangeType.Direct);
        GetRespondReadyJobQueue.ExchangeDeclare(exchange: exchange, type: ExchangeType.Direct);
        GetFinishedReadyJobQueue.ExchangeDeclare(exchange: exchange, type: ExchangeType.Direct);

        GetRequestReadyJobQueue.QueueDeclare(queue: RequestReadyJobName, durable: false, exclusive: false, autoDelete: false, arguments: null);
        GetRespondReadyJobQueue.QueueDeclare(queue: RespondReadyJobName, durable: false, exclusive: false, autoDelete: false, arguments: null);
        GetFinishedReadyJobQueue.QueueDeclare(queue: RespondFinishedJobName, durable: false, exclusive: false, autoDelete: false, arguments: null);

    }

    public IModel GetConnection()
    {
        return _connection.CreateModel();
    }

}