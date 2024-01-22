using MessagePack;
using Microservice.Domain;
using Microservice.TaskQueueManage.Infrastructure;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microservice.TaskManagQueueManager.Infrastructure;

internal class GetJobQueueService
{
    private readonly ILogger<GetJobQueueService> _logger;

    private readonly RabbitMQService _rabbitMQ;

    private IModel? _requestChannel;
    private IModel? _responseChannel;

    public int _activeJobs { get; set; }

    public GetJobQueueService(
        ILogger<GetJobQueueService> logger,
        RabbitMQService rabbitMQ,
        IHostApplicationLifetime lifetime)
    {
        _logger = logger;
        _rabbitMQ = rabbitMQ;
        _requestChannel = _rabbitMQ.GetConnection();
        _responseChannel = _rabbitMQ.GetConnection();
        SetupRequestAndResponseListener(lifetime.ApplicationStopping);
    }

    public void SendJobRequestAsync(JobRequest jobRequest)
    {
        try
        {
            var requestSerialized = MessagePackSerializer.Serialize(jobRequest);
            _requestChannel.BasicPublish(
                exchange: _rabbitMQ.exchange,
                routingKey: _rabbitMQ.getRequestQueueName,
                basicProperties: null,
                body: requestSerialized);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending job request");
        }
    }

    private void SetupRequestAndResponseListener(CancellationToken cancellationToken)
    {
        var responseConsumer = new EventingBasicConsumer(_responseChannel);
        responseConsumer.Received += async (model, ea) =>
        {
            try
            {
                var response = await MessagePackSerializer
                    .DeserializeAsync<JobResponse>(new MemoryStream(ea.Body.ToArray()));

                // Process the received response
                // For example, updating job status, logging, etc.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetJobQueueService response listener");
            }
        };
        _responseChannel.BasicConsume(queue: _rabbitMQ.getResponseQueueName, autoAck: true, consumer: responseConsumer);
    }

    private async Task SimulateWork(JobResponse jobResponse)
    {
        _logger.LogInformation("JobhistoryId {JobhistoryId} with Address {Address} has begun processing", jobResponse.JobHistoryId, jobResponse.Address);
        await Task.Delay(TimeSpan.FromSeconds(10));
        _logger.LogInformation("JobhistoryId {JobhistoryId} with Address {Address} has stopped processing", jobResponse.JobHistoryId, jobResponse.Address);

    }
}
