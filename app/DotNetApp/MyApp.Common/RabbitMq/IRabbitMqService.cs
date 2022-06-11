namespace MyApp.Common.RabbitMq;

public interface IRabbitMqService
{
    void PublishEvent(IEvent obj);
    void SendCommand(ICommand obj);
}