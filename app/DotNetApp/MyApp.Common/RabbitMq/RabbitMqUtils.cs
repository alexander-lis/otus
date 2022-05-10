using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MyApp.Common.RabbitMq;

public static class RabbitMqUtils
{
    public static void InitializeServiceQueueForMessageType(IModel model, Type type, ServiceName serviceName)
    {
        var exchangeName = GetExchangeName(type);
        var queueName = GetQueueName(type, serviceName);
        var routingKey = GetRoutingKey(type);

        ExchangeDeclare(model, exchangeName);
        QueueDeclare(model, queueName);
        model.QueueBind(queueName, exchangeName, routingKey);    
    }

    public static void ExchangeDeclare(IModel model, string exchangeName)
    {
        model.ExchangeDeclare(exchangeName, ExchangeType.Direct);
    }

    public static void QueueDeclare(IModel model, string queueName)
    {
        model.QueueDeclare(queueName, false, true, false);
    }
    
    public static string GetExchangeName(Type type)
    {
        return type.FullName!;
    }

    public static string GetQueueName(Type type, ServiceName serviceName)
    {
        return GetExchangeName(type) + ".To" + Enum.GetName(serviceName);
    }

    public static string GetRoutingKey(Type type)
    {
        return GetExchangeName(type) + ".*";
    }

    public static T DeserealizeMessage<T>(BasicDeliverEventArgs ea)
    {
        var body = ea.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);
        var content = JsonConvert.DeserializeObject<T>(message);

        if (content is null)
        {
            throw new InvalidDataException(typeof(T) + " message is empty!");
        }
        
        return content;
    }
}