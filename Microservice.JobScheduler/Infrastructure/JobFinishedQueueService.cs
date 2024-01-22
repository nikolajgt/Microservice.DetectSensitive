﻿using MessagePack;
using Microservice.Domain;
using Microservice.Domain.Models.JobModels;
using Microservice.JobScheduler.Application.Validation;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Microservice.JobScheduler.Infrastructure;


internal class JobFinishedQueueService : IDisposable
{
    private readonly ILogger<GetJobQueueService> _logger;

    private readonly JobFinishedResponseValidator _jobResponseValidator = new JobFinishedResponseValidator();

    private readonly JobSchedulerService _jobSchedulerService;
    private readonly RabbitMQService _rabbitMQ;

    private IModel? _requestChannel;
    private IModel? _responseChannel;

    public JobFinishedQueueService(
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
        SetupJobFinishedResponseListener(lifetime.ApplicationStopping);
    }


    private void SetupJobFinishedResponseListener(CancellationToken cancellationToken)
    {
        var consumer = new EventingBasicConsumer(_requestChannel);
        consumer.Received += async (model, ea) =>
        {
            try
            {
                // Process the request and prepare a job response
                var request = await MessagePackSerializer
                    .DeserializeAsync<JobFinishedResponse>(new MemoryStream(ea.Body.ToArray()));

                var validatedRequest = await _jobResponseValidator.ValidateAsync(request);
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