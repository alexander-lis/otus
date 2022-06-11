using MyApp.Common.RabbitMq;

namespace MyApp.Notifications.Contract.Events;

public class OrderPaymentDeclined : IEvent
{
    public int OrderId { get; }

    public OrderPaymentDeclined(int orderId)
    {
        OrderId = orderId;
    }
}