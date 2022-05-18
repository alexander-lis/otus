using MyApp.Common.RabbitMq;

namespace MyApp.Orders.Contract.Events;

public class OrderCreated : IEvent
{
    public OrderCreated(int userId, int orderId, string orderOrderTitle, int orderOrderPrice)
    {
        UserId = userId;
        OrderId = orderId;
        OrderTitle = orderOrderTitle;
        OrderPrice = orderOrderPrice;
    }

    public int UserId { get; }
    public int OrderId { get; }
    public string OrderTitle { get; }
    public int OrderPrice { get; }
}