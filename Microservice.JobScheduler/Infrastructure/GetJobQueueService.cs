﻿using MessagePack;
using Microservice.Domain;
using Microservice.JobScheduler.Application.Validation;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Threading.Channels;

namespace Microservice.JobScheduler.Infrastructure;

internal class GetJobQueueService : IDisposable
{
    private readonly ILogger<GetJobQueueService> _logger;

    private readonly JobRequestValidator _jobRequestValidator = new JobRequestValidator();
    private readonly JobResponseValidator _jobResponseValidator = new JobResponseValidator();

    private readonly JobSchedulerService _jobSchedulerService;
    private readonly RabbitMQService _rabbitMQ;

    private IModel? _requestChannel;
    private IModel? _responseChannel;

    public GetJobQueueService(
        ILogger<GetJobQueueService> logger,
        RabbitMQService rabbitMQ,
        JobSchedulerService jobSchedulerService,
        IHostApplicationLifetime lifetime)
    {
        _logger = logger;
        _rabbitMQ = rabbitMQ;
        _jobSchedulerService = jobSchedulerService;
        _requestChannel = _rabbitMQ.GetConnection();
        _responseChannel = _rabbitMQ.GetConnection();
        SetupRequestAndResponseListener(lifetime.ApplicationStopping);
    }

    private void SetupRequestAndResponseListener(CancellationToken cancellationToken)
    {
        var consumer = new EventingBasicConsumer(_requestChannel);
        consumer.Received += async (model, ea) =>
        {
            try
            {
                // Process the request and prepare a job response
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
                _responseChannel.BasicPublish(
                    exchange: _rabbitMQ.exchange,
                    routingKey: _rabbitMQ.jobSchedulerResponseQueueName,
                    basicProperties: null,
                    body: responseSerialized);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetJobQueueService SetupRequestListener");
            }
        };
        _requestChannel.BasicConsume(queue: _rabbitMQ.jobSchedulerRequestQueueName, autoAck: true, consumer: consumer);
    }

    public void Dispose()
    {
        _requestChannel?.Close();
        _requestChannel?.Dispose();
        _responseChannel?.Close();
        _responseChannel?.Dispose();
    }
}
