using MyApp.Common.RabbitMq;

namespace MyApp.Orders.Contract.Events;

public class OrderCreated : IEvent
{
    public int UserId { get; }

    public int OrderId { get; }

    public string OrderTitle { get; }

    public int OrderPrice { get; }

    public OrderCreated(int userId, int orderId, string orderTitle, int orderPrice)
    {
        UserId = userId;
        OrderId = orderId;
        OrderTitle = orderTitle;
        OrderPrice = orderPrice;
    }
}