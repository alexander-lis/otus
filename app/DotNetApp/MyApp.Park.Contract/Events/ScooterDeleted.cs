using MyApp.Common.RabbitMq;

namespace MyApp.Park.Contract.Events;

public class ScooterDeleted : IEvent
{
    public int Id { get; }

    public ScooterDeleted(int id)
    {
        Id = id;
    }
}