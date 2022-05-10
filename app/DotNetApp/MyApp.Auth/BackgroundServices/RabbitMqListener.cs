using MyApp.Auth.Contract.Events;
using MyApp.Common.RabbitMq;
using MyApp.Notifications.Contract.Commands;
using RabbitMQ.Client;

namespace MyApp.Auth.BackgroundServices;

public class RabbitMqListener : BackgroundService
{
    private readonly IModel _model;

    public RabbitMqListener(IConnectionFactory connectionFactory)
    {
        var connection = connectionFactory.CreateConnection();
        _model = connection.CreateModel();
        
        Initialize();
    }

    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.CompletedTask;
    }

    private void Initialize()
    {
        RabbitMqUtils.ExchangeDeclare(_model, RabbitMqUtils.GetExchangeName(typeof(UserCreated)));
        RabbitMqUtils.ExchangeDeclare(_model, RabbitMqUtils.GetExchangeName(typeof(NotifyUserCreated)));
    }
}
