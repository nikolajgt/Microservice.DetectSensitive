using MessagePack;
using MessagePack.Resolvers;
using Microservice.Domain;
using Microservice.Domain.Models.JobModels;
using Microservice.TaskQueueManage.Infrastructure;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Microservice.TaskManagQueueManager.Infrastructure;

public class GetJobQueueListenerService
{
    private readonly ILogger<GetJobQueueListenerService> _logger;

    private readonly RabbitMQService _rabbitMQ;

    private IModel? _requestChannel;
    private IModel? _responseChannel;

    public int _activeJobs { get; set; }

    public GetJobQueueListenerService(
        ILogger<GetJobQueueListenerService> logger,
        RabbitMQService rabbitMQ,
        IHostApplicationLifetime lifetime)
    {
        _logger = logger;
        _rabbitMQ = rabbitMQ;
        _requestChannel = _rabbitMQ.GetConnection();
        _responseChannel = _rabbitMQ.GetConnection();
        _requestChannel.ExchangeDeclare(exchange: _rabbitMQ.exchange, type: ExchangeType.Direct);
        _responseChannel.ExchangeDeclare(exchange: _rabbitMQ.exchange, type: ExchangeType.Direct);
        _requestChannel.QueueDeclare(queue: _rabbitMQ.QueueRequestReadyJob, durable: false, exclusive: false, autoDelete: false, arguments: null);
        _responseChannel.QueueDeclare(queue: _rabbitMQ.QueueRespondReadyJob, durable: false, exclusive: false, autoDelete: false, arguments: null);
        SetupResponseListener(lifetime.ApplicationStopping);
    }

    public void SendJobRequestAsync(JobRequest jobRequest)
    {
        try
        {
            var options = MessagePackSerializerOptions.Standard.WithResolver(ContractlessStandardResolver.Instance);
            var requestSerialized = MessagePackSerializer.Serialize(jobRequest, options);
            _requestChannel.BasicPublish(
                exchange: _rabbitMQ.exchange,
                routingKey: _rabbitMQ.QueueRequestReadyJob,
                basicProperties: null,
                body: requestSerialized);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending job request at SendJobRequestAsync in GetJobQueueListenerService");
        }
    }

    private void SetupResponseListener(CancellationToken cancellationToken)
    {
        var responseConsumer = new EventingBasicConsumer(_responseChannel);
        responseConsumer.Received += async (model, ea) =>
        {
            try
            {
                var response = await MessagePackSerializer
                    .DeserializeAsync<JobResponse>(new MemoryStream(ea.Body.ToArray()));

                // Corrct way, delegate the incomming request to correct queue
                await SimulateWork(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetJobQueueService response listener");
            }
        };
        _responseChannel.BasicConsume(queue: _rabbitMQ.QueueRespondReadyJob, autoAck: true, consumer: responseConsumer);
    }

    private async Task SimulateWork(JobResponse jobResponse)
    {
        _logger.LogInformation("JobhistoryId {JobhistoryId} with Address {Address} has begun processing", jobResponse.JobHistoryId, jobResponse.Address);
        await Task.Delay(TimeSpan.FromSeconds(10));
        _logger.LogInformation("JobhistoryId {JobhistoryId} with Address {Address} has finished processing", jobResponse.JobHistoryId, jobResponse.Address);
        //UpdateJob(new JobFinishedResponse
        //{
        //    JobHistoryId = jobResponse.JobHistoryId,
        //    Success = true,
        //});
    }

    //private void UpdateJob(JobFinishedResponse response)
    //{
    //    var requestSerialized = MessagePackSerializer.Serialize(response);
    //    _requestChannel.BasicPublish(
    //        exchange: _rabbitMQ.exchange,
    //        routingKey: _rabbitMQ.QueueRespondFinishedJob,
    //        basicProperties: null,
    //        body: requestSerialized);
    //}
}
