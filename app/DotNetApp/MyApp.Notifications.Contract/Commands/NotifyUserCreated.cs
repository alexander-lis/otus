using MyApp.Common.RabbitMq;

namespace MyApp.Notifications.Contract.Commands;

public class NotifyUserCreated : ICommand
{
    public int UserId { get; }
    public string Email { get; }

    public NotifyUserCreated(int userId, string email)
    {
        UserId = userId;
        Email = email;
    }
}