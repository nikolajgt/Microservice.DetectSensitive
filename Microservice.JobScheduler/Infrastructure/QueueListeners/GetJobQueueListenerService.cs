using MessagePack;
using Microservice.Domain;
using Microservice.Domain.Models.JobModels;
using Microservice.JobScheduler.Application.Validation;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Threading.Channels;

namespace Microservice.JobScheduler.Infrastructure.QueueListeners;

public class GetJobQueueListenerService : IDisposable
{
    private readonly ILogger<GetJobQueueListenerService> _logger;

    private readonly JobRequestValidator _jobRequestValidator = new JobRequestValidator();
    private readonly JobResponseValidator _jobResponseValidator = new JobResponseValidator();
    private readonly JobFinishedResponseValidator _jobSendResponseValidator = new JobFinishedResponseValidator();

    private readonly JobSchedulerService _jobSchedulerService;
    private readonly RabbitMQService _rabbitMQ;

    private IModel? _requestJobReadyChannel;
    private IModel? _responseJobReadyChannel;
    private IModel? _responseJobFinishedChannel;

    public GetJobQueueListenerService(
        ILogger<GetJobQueueListenerService> logger,
        RabbitMQService rabbitMQ,
        JobSchedulerService jobSchedulerService,
        IHostApplicationLifetime lifetime)
    {
        _logger = logger;
        _rabbitMQ = rabbitMQ;
        _jobSchedulerService = jobSchedulerService;
        _requestJobReadyChannel = _rabbitMQ.GetConnection();
        _responseJobReadyChannel = _rabbitMQ.GetConnection();
        _responseJobFinishedChannel = _rabbitMQ.GetConnection();
        _requestJobReadyChannel.ExchangeDeclare(exchange: _rabbitMQ.exchange, type: ExchangeType.Direct);
        _responseJobReadyChannel.ExchangeDeclare(exchange: _rabbitMQ.exchange, type: ExchangeType.Direct);
        _responseJobFinishedChannel.ExchangeDeclare(exchange: _rabbitMQ.exchange, type: ExchangeType.Direct);
        
        _requestJobReadyChannel.QueueDeclare(queue: _rabbitMQ.QueueRequestReadyJob, durable: false, exclusive: false, autoDelete: false, arguments: null);
        _responseJobReadyChannel.QueueDeclare(queue: _rabbitMQ.QueueRespondFinishedJob, durable: false, exclusive: false, autoDelete: false, arguments: null);
        _responseJobFinishedChannel.QueueDeclare(queue: _rabbitMQ.QueueRespondFinishedJob, durable: false, exclusive: false, autoDelete: false, arguments: null);

        SetupRequestAndResponseListener(lifetime.ApplicationStopping);
    }


    private void SetupRequestAndResponseListener(CancellationToken cancellationToken)
    {
        var consumer = new EventingBasicConsumer(_requestJobReadyChannel);
        consumer.Received += async (model, ea) =>
        {
            try
            {
                // Process the request and prepare a job response
                _logger.LogInformation("Request came through");
                var request = await MessagePackSerializer
                    .DeserializeAsync<JobRequest>(new MemoryStream(ea.Body.ToArray()));

                var validatedRequest = await _jobRequestValidator.ValidateAsync(request);
                if (!validatedRequest.IsValid)
                {
                    _logger.LogError("Job scheduler got a request with empty target");
                    return;
                }

                var jobHistory = await _jobSchedulerService.DequeueNextReadyJobAsync(request.HarvesterType, cancellationToken);
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
                _responseJobReadyChannel.BasicPublish(
                    exchange: _rabbitMQ.exchange,
                    routingKey: _rabbitMQ.QueueRespondReadyJob,
                    basicProperties: null,
                    body: responseSerialized);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetJobQueueService SetupRequestListener");
            }
        };
        _requestJobReadyChannel.BasicConsume(queue: _rabbitMQ.QueueRequestReadyJob, autoAck: true, consumer: consumer);
    }

    private void SetupJobFinishedResponseListener(CancellationToken cancellationToken)
    {
        var consumer = new EventingBasicConsumer(_responseJobReadyChannel);
        consumer.Received += async (model, ea) =>
        {
            try
            {
                // Process the request and prepare a job response
                var request = await MessagePackSerializer
                    .DeserializeAsync<JobFinishedResponse>(new MemoryStream(ea.Body.ToArray()));

                var validatedRequest = await _jobSendResponseValidator.ValidateAsync(request);
                if (!validatedRequest.IsValid)
                {
                    _logger.LogError("Job scheduler got a request with empty target");
                    return;
                }

                await _jobSchedulerService.UpdateJobHistory(request.JobHistoryId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in JobFinishedQueueService SetupRequestAndResponseListener");
            }
        };
        _responseJobFinishedChannel.BasicConsume(queue: _rabbitMQ.QueueRespondFinishedJob, autoAck: true, consumer: consumer);
    }

    public void Dispose()
    {
        _requestJobReadyChannel?.Close();
        _requestJobReadyChannel?.Dispose();
        _responseJobReadyChannel?.Close();
        _responseJobReadyChannel?.Dispose();
        _responseJobFinishedChannel?.Close();
        _responseJobFinishedChannel?.Dispose();
    }
}
