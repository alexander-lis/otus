using MyApp.Auth.Contract.Events;
using MyApp.Common.RabbitMq;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MyApp.Billing.BackgroundServices;

public class RabbitMqListener : BackgroundService
{
    private readonly IModel _model;

    public RabbitMqListener(IConnectionFactory connectionFactory)
    {
        var connection = connectionFactory.CreateConnection();
        _model = connection.CreateModel();
        
        Initialize();
    }

    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ConsumeUserCreatedEvent(_model);
        return Task.CompletedTask;
    }

    private void ConsumeUserCreatedEvent(IModel model)
    {
        var consumer = new EventingBasicConsumer(model);
        consumer.Received += (ch, ea) =>
        {
            var content = JsonConvert.DeserializeObject<UserCreated>(ea.Body.ToString());
            model.BasicAck(ea.DeliveryTag, false);
        };

        var queueName = RabbitMqUtils.GetQueueName(typeof(UserCreated), ServiceName.Billing);
        model.BasicConsume(queueName, false, consumer);
    }
    
    private void Initialize()
    {
        RabbitMqUtils.InitializeServiceQueueForMessageType(_model, typeof(UserCreated), ServiceName.Billing);
    }
}