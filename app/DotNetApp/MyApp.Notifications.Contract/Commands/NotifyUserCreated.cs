using MyApp.Common.RabbitMq;

namespace MyApp.Notifications.Contract.Commands;

public class NotifyUserCreated : ICommand
{
    public NotifyUserCreated(int id, string email)
    {
        Id = id;
        Email = email;
    }

    public int Id { get; }

    public string Email { get; }
}