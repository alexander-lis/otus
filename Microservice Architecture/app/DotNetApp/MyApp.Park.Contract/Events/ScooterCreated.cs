using MyApp.Common.RabbitMq;

namespace MyApp.Park.Contract.Events;

public class ScooterCreated : IEvent
{
    public int Id { get; }
    public string Name { get; }
    
    public ScooterCreated(int id, string name)
    {
        Id = id;
        Name = name;
    }
}