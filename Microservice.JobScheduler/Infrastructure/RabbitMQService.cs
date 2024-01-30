﻿using RabbitMQ.Client;

namespace Microservice.JobScheduler.Infrastructure;

public class RabbitMQService
{
    private readonly ILogger<RabbitMQService> _logger;
    private readonly IConnection _connection;
    public IChannel? GetRequestReadyJobQueue { get; }
    public IChannel? GetRespondReadyJobQueue { get; }
    public IChannel? GetFinishedReadyJobQueue { get; }

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
        //GetRequestReadyJobQueue = _connection.CreateChannelAsync();
        //GetRespondReadyJobQueue = _connection.CreateChannelAsync();
        //GetFinishedReadyJobQueue = _connection.CreateChannelAsync();

        //GetRequestReadyJobQueue.ExchangeDeclare(exchange: exchange, type: ExchangeType.Direct);
        //GetRespondReadyJobQueue.ExchangeDeclare(exchange: exchange, type: ExchangeType.Direct);
        //GetFinishedReadyJobQueue.ExchangeDeclare(exchange: exchange, type: ExchangeType.Direct);

        
        
        //GetFinishedReadyJobQueue.QueueDeclare(queue: RespondFinishedJobName, durable: false, exclusive: false, autoDelete: false, arguments: null);
    }

    public async Task<IChannel> CreateChannelAsync()
    {
        return await _connection.CreateChannelAsync();
    }


}