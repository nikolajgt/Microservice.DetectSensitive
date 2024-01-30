using MessagePack;
using Microservice.Domain;
using Microservice.Domain.Models.JobModels;
using Microservice.JobScheduler.Application.Validation;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Threading.Channels;

namespace Microservice.JobScheduler.Infrastructure.QueueListeners;

public class GetJobQueueListenerService
{
    private readonly ILogger<GetJobQueueListenerService> _logger;


    private readonly JobFinishedResponseValidator _jobSendResponseValidator = new JobFinishedResponseValidator();

    private readonly JobSchedulerService _jobSchedulerService;
    private readonly RabbitMQService _rabbitMQ;

    private readonly IChannel ReciveJobRequest;
    private readonly IChannel SendJobRequest;
    private readonly IChannel ReciveFinishedJob;
    private readonly IChannel TestListener;


    public GetJobQueueListenerService(
        ILogger<GetJobQueueListenerService> logger,
        RabbitMQService rabbitMQ,
        JobSchedulerService jobSchedulerService,
        IHostApplicationLifetime lifetime)
    {
        _logger = logger;
        _rabbitMQ = rabbitMQ;
        _jobSchedulerService = jobSchedulerService;
        SetupRequestAndResponseListener(lifetime.ApplicationStopping);
    }


    private async Task TestListenerFunc(CancellationToken cancellationToken)
    {
        await TestListener.QueueDeclareAsync(queue: "hello",
                             durable: false,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);

        var consumer = new EventingBasicConsumer(TestListener);
        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            _logger.LogInformation($" [x] Received {message}");
        };
        TestListener.BasicConsumeAsync(queue: "hello",
                             autoAck: true,
                             consumer: consumer);
    }

    private void SetupRequestAndResponseListener(CancellationToken cancellationToken)
    {
        var consumer = new EventingBasicConsumer(_rabbitMQ.GetRequestReadyJobQueue);
        consumer.Received += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
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
                _rabbitMQ.GetRespondReadyJobQueue.BasicPublish(
                    exchange: _rabbitMQ.exchange,
                    routingKey: _rabbitMQ.RespondReadyJobName,
                    basicProperties: null,
                    body: responseSerialized);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetJobQueueService SetupRequestListener");
            }
        };
        _rabbitMQ.GetRequestReadyJobQueue.BasicConsume(queue: _rabbitMQ.RequestReadyJobName, autoAck: true, consumer: consumer);
    }


}
