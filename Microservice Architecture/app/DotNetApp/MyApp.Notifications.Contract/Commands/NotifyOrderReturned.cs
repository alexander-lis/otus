using MyApp.Common.RabbitMq;

namespace MyApp.Notifications.Contract.Commands;

public class NotifyOrderReturned : ICommand
{
    public int UserId { get; }
    public int OrderId { get; }
    public string OrderTitle { get; }

    public NotifyOrderReturned(int userId, int orderId, string orderTitle)
    {
        UserId = userId;
        OrderId = orderId;
        OrderTitle = orderTitle;
    }
}