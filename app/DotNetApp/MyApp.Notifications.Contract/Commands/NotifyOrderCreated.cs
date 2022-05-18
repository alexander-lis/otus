using MyApp.Common.RabbitMq;

namespace MyApp.Notifications.Contract.Commands;

public class NotifyOrderCreated : ICommand
{
    public int UserId { get; }
    public int OrderId { get; }
    public string OrderTitle { get; }
    public int OrderPrice { get; }

    public NotifyOrderCreated(int userId, int orderId, string orderTitle, int orderPrice)
    {
        UserId = userId;
        OrderId = orderId;
        OrderTitle = orderTitle;
        OrderPrice = orderPrice;
    }
}