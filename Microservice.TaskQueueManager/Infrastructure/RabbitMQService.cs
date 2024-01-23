using RabbitMQ.Client;

namespace Microservice.TaskQueueManage.Infrastructure;

public class RabbitMQService
{
    private readonly ILogger<RabbitMQService> _logger;
    private readonly IConnection _connection;
    public string exchange { get; } = "DataHarvest";
    public string QueueRequestReadyJob { get; } = "DataHarvest_RequestReadyJob";
    public string QueueRespondReadyJob { get; } = "DataHarvest_RespondReadyJob";
    public string QueueRespondFinishedJob { get; } = "DataHarvest_RespondFinishedJob";

    public RabbitMQService(
        ILogger<RabbitMQService> logger,
        IHostApplicationLifetime lifetime)
    {
        _logger = logger;
        var connectionFactory = new ConnectionFactory
        {
            HostName = "rabbitmq", // RabbitMQ server host
            Port = 5672,            // RabbitMQ server port
        };
        _connection = connectionFactory.CreateConnection();

    }

    public IModel GetConnection()
    {
        return _connection.CreateModel();
    }

}
