using System.Transactions;
using Dapper;
using MyApp.Common.RabbitMq;
using MyApp.Notifications.Contract.Commands;
using MySqlConnector;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MyApp.Notifications.BackgroundServices;

public class RabbitMqListener : BackgroundService
{
    // Поля.
    private readonly IModel _model;
    private readonly MySqlConnection _dbConnection;
    
    // Скрипты.
    // Скрипты.
    private readonly Func<NotifyUserCreated, DateTime, string> _insertNotificationLogSql =
        (command, now) =>
            $"INSERT INTO notification_log (user_id, email, datetime) VALUES ('{command.Id}', '{command.Email}', '{now.ToString("yyyy-MM-dd H:mm:ss")}'); SELECT LAST_INSERT_ID()";

    public RabbitMqListener(IConnectionFactory connectionFactory, MySqlConnection dbConnection)
    {
        var rmqConnection = connectionFactory.CreateConnection();
        _model = rmqConnection.CreateModel();
        _dbConnection = dbConnection;

        Initialize();
    }

    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ConsumeNotifyUserCreatedCommand();
        return Task.CompletedTask;
    }

    private void ConsumeNotifyUserCreatedCommand()
    {
        var consumer = new EventingBasicConsumer(_model);
        consumer.Received += (ch, ea) =>
        {
            using var transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            var datetime = DateTime.UtcNow;
            var command = RabbitMqUtils.DeserealizeMessage<NotifyUserCreated>(ea);
            var sql = _insertNotificationLogSql(command, datetime);
            _dbConnection.Execute(sql);
            _model.BasicAck(ea.DeliveryTag, false);
            transactionScope.Complete();
            Console.WriteLine("Notification sent to " + command.Email);
        };

        var queueName = RabbitMqUtils.GetQueueName(typeof(NotifyUserCreated), ServiceName.Notifications);
        _model.BasicConsume(queueName, false, consumer);
    }

    private void Initialize()
    {
        RabbitMqUtils.InitializeServiceQueueForMessageType(_model, typeof(NotifyUserCreated), ServiceName.Notifications);
    }
}