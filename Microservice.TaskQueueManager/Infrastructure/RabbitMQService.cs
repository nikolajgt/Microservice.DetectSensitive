using RabbitMQ.Client;

namespace Microservice.TaskQueueManage.Infrastructure;

internal class RabbitMQService
{
    private readonly ILogger<RabbitMQService> _logger;
    private readonly IConnection _connection;
    public string exchange { get; } = "DataHarvest";
    public string getRequestQueueName { get; } = "DataHarvest_JobSchedulerGetRequestQueue";
    public string getResponseQueueName { get; } = "DataHarvest_JobSchedulerGetResponseQueue";
    public string jobFinishedResponseQueueName { get; } = "DataHarvest_JobSchedulerGetResponseQueue";

    public RabbitMQService(
        ILogger<RabbitMQService> logger,
        IHostApplicationLifetime lifetime)
    {
        _logger = logger;
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
