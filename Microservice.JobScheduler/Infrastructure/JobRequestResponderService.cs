using MessagePack;
using Microservice.Domain;
using Microservice.JobScheduler.Application.Validation;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Microservice.JobScheduler.Infrastructure.QueueListeners;

internal class JobRequestResponderService : BackgroundService
{
    private readonly ILogger<JobRequestResponderService> _logger;
    private readonly RabbitMQService _rabbitMQ;
    private readonly JobSchedulerService _scheduler;

    private readonly JobRequestValidator _jobRequestValidator = new JobRequestValidator();
    private readonly JobResponseValidator _jobResponseValidator = new JobResponseValidator();

    private IChannel? _jobRequestQueue;
    private IChannel? _jobRespondQueue;

    public JobRequestResponderService(
        ILogger<JobRequestResponderService> logger,
        RabbitMQService rabbitMQ,
        JobSchedulerService jobScheduler)
    {
        _logger = logger;
        _rabbitMQ = rabbitMQ;
        _scheduler = jobScheduler;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await SetupQueues();
        await SetupJobRequestResponderAsync(cancellationToken);
    }

    private async Task SetupJobRequestResponderAsync(CancellationToken cancellationToken)
    {
        var consumer = new EventingBasicConsumer(_jobRequestQueue);
        consumer.Received += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                _logger.LogInformation("Request came through");

                var request = await MessagePackSerializer
                    .DeserializeAsync<JobRequest>(new MemoryStream(ea.Body.ToArray()));

                var validatedRequest = await _jobRequestValidator.ValidateAsync(request);
                if (!validatedRequest.IsValid)
                {
                    _logger.LogError("Job scheduler got a request with empty target");
                    return;
                }

                var jobHistory = await _scheduler.DequeueNextReadyJobAsync(request.HarvesterType, cancellationToken);
                var response = new JobResponse
                {
                    JobHistoryId = jobHistory.Id,
                    Address = jobHistory.Job.Source.Address,
                };

                var validatedResponse = await _jobResponseValidator.ValidateAsync(response);
                if (!validatedResponse.IsValid)
                {
                    _logger.LogError("Job scheduler created a response with empty paramters jobHistoryId: {jobHistoryId}", jobHistory.Id);
                    return;
                }

                var responseSerialized = MessagePackSerializer.Serialize(response);
                await _jobRespondQueue.BasicPublishAsync(
                    exchange: _rabbitMQ.exchange,
                    routingKey: _rabbitMQ.RespondReadyJobName,
                    body: responseSerialized);
                // ack that we are done
                await _jobRequestQueue.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetJobQueueService SetupRequestListener");
            }
        };
        await _jobRequestQueue.BasicConsumeAsync(
            queue: _rabbitMQ.RequestReadyJobName, 
            autoAck: false,
            consumer: consumer);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);

        await _jobRequestQueue.CloseAsync();
        await _jobRespondQueue.CloseAsync();
    }


    private async Task SetupQueues()
    {
        try
        {
            _jobRequestQueue = await _rabbitMQ.CreateChannelAsync();
            _jobRespondQueue = await _rabbitMQ.CreateChannelAsync();

            //_jobRequestQueue.ExchangeDeclare(exchange: "DataHarvest", type: "direct");
            //_jobRespondQueue.ExchangeDeclare(exchange: "DataHarvest", type: "direct");

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
