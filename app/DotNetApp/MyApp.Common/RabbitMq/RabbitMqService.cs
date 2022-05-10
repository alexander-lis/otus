namespace MyApp.Common.RabbitMq;

using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

public class RabbitMqService : IRabbitMqService
{
    private readonly IConnectionFactory _connectionFactory;

    public RabbitMqService(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public void PublishEvent(IEvent obj)
    {
        Publish(obj);
    }

    public void PublishCommand(ICommand obj)
    {
        Publish(obj);
    }

    private void Publish(object obj)
    {
        var type = obj.GetType();
        var message = JsonSerializer.Serialize(obj);
        var exchangeName = RabbitMqUtils.GetExchangeName(type);
        var routingKey = RabbitMqUtils.GetRoutingKey(type);

        using var connection = _connectionFactory.CreateConnection();
        using var channel = connection.CreateModel();

        var body = Encoding.UTF8.GetBytes(message);

        channel.BasicPublish(exchangeName, routingKey, null, body);
    }
}