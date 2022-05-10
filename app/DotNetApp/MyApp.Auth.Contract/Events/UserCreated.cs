using MyApp.Common.RabbitMq;

namespace MyApp.Auth.Contract.Events;

public class UserCreated : IEvent
{
    public UserCreated(int id, string email)
    {
        Id = id;
        Email = email;
    }

    public int Id { get; }

    public string Email { get; }
}