using MessagePack.Resolvers;
using MessagePack;
using Microservice.Domain;
using Microservice.Domain.Base.Enums;
using Microservice.TaskQueueManage.Infrastructure;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microservice.TaskManagQueueManager.Application.IServices;
using System.Threading.Channels;

namespace Microservice.TaskManagQueueManager.Infrastructure.Queues;

public class JobRequestResponderService : BackgroundService, IJobRequestResponderService
{
    private readonly ILogger<JobRequestResponderService> _logger;
    private readonly RabbitMQService _rabbitMQ;

    //private readonly JobRequestValidator _jobRequestValidator = new JobRequestValidator();
    //private readonly JobResponseValidator _jobResponseValidator = new JobResponseValidator();

    private IChannel? _jobRequestQueue;
    private IChannel? _jobRespondQueue;
    private bool _isInitialized = false;
    public JobRequestResponderService(
    ILogger<JobRequestResponderService> logger,
    RabbitMQService rabbitMQ)
    {
        _logger = logger;
        _rabbitMQ = rabbitMQ;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await SetupQueues();
        _isInitialized = true;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);

        await _jobRequestQueue.CloseAsync();
        await _jobRespondQueue.CloseAsync();
    }

    public async Task ReqeustJobAsync(HarvesterType harvesterType)
    {
        try
        {
            if (_jobRespondQueue.IsOpen)
                _logger.LogInformation("Connection is open");

            var request = new JobRequest
            {
                HarvesterType = harvesterType,
            };

            var options = MessagePackSerializerOptions.Standard.WithResolver(ContractlessStandardResolver.Instance);
            var serializedBody = MessagePackSerializer.Serialize(request, options);
            await _jobRequestQueue.BasicPublishAsync(
                exchange: _rabbitMQ.exchange,
                routingKey: _rabbitMQ.RequestReadyJobName,
                body: serializedBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending job request at SendJobRequestAsync in GetJobQueueListenerService");
        }
    }

    private async Task SetupQueues()
    {
        try
        {
            _jobRequestQueue = await _rabbitMQ.CreateChannelAsync();
            _jobRespondQueue = await _rabbitMQ.CreateChannelAsync();

            _jobRequestQueue.ExchangeDeclare(exchange: "DataHarvest", type: "direct");
            _jobRespondQueue.ExchangeDeclare(exchange: "DataHarvest", type: "direct");

            await _jobRequestQueue.QueueDeclareAsync(
                queue: _rabbitMQ.RequestReadyJobName,
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            await _jobRespondQueue.QueueDeclareAsync(
                queue: _rabbitMQ.RespondReadyJobName,
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Setup Queues failing, can get connection/queue to rabbitmq: Function {function} in Class {class}",
                nameof(SetupQueues),
                nameof(JobRequestResponderService));
        }
    }
}
