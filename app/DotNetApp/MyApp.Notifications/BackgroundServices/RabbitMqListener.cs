using System.Transactions;
using Dapper;
using MyApp.Common.RabbitMq;
using MyApp.Notifications.Contract.Commands;
using MyApp.Notifications.Models;
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
    private readonly Func<int, string, string, DateTime, string> _insertNotificationMessageSql =
        (userId, email, text, now) =>
            $"INSERT INTO notification_history (userid, email, message, datetime) VALUES ('{userId}', '{email}', '{text}', '{now.ToString("yyyy-MM-dd H:mm:ss")}'); SELECT LAST_INSERT_ID()";

    private readonly Func<int, string, string> _insertRecipientSql =
        (userId, email) =>
            $"INSERT INTO notification_recipients (userid, email) VALUES ('{userId}', '{email}')";

    private readonly Func<int, string> _getRecipientSql =
        (userId) =>
            $"SELECT * FROM notification_recipients WHERE userid = {userId}";
    
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
        ConsumeCommands();
        return Task.CompletedTask;
    }

    private void ConsumeCommands()
    {
        // NotifyUserCreated.
        var userCreatedConsumer = new EventingBasicConsumer(_model);
        userCreatedConsumer.Received += (ch, ea) =>
        {
            Console.WriteLine("Notifications: NotifyUserCreated command received.");

            using (var transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var datetime = DateTime.UtcNow;
                var command = RabbitMqUtils.DeserealizeMessage<NotifyUserCreated>(ea);
            
                // Insert recipient.
                var sql = _insertRecipientSql(command.UserId, command.Email);
                _dbConnection.Execute(sql);
            
                // Send message.
                var message = $"You are registered!";
                sql = _insertNotificationMessageSql(command.UserId, command.Email, message, datetime);
                _dbConnection.Execute(sql);

                transactionScope.Complete();
            }
            
            // Ack.
            _model.BasicAck(ea.DeliveryTag, false);
        };

        // NotifyOrderCreated.
        var orderCreatedConsumer = new EventingBasicConsumer(_model);
        orderCreatedConsumer.Received += (ch, ea) =>
        {
            Console.WriteLine("Notifications: NotifyOrderCreated command received.");

            using (var transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var datetime = DateTime.UtcNow;
                var command = RabbitMqUtils.DeserealizeMessage<NotifyOrderCreated>(ea);
            
                // Get recipient.
                var sql = _getRecipientSql(command.UserId);
                var recipient = _dbConnection.QueryFirst<Recipient>(sql);
            
                // Send message.
                var message = $"You have new order {command.OrderTitle} with price {command.OrderPrice}";
                sql = _insertNotificationMessageSql(recipient.UserId, recipient.Email, message, datetime);
                _dbConnection.Execute(sql);
                transactionScope.Complete();
            }
            
            _model.BasicAck(ea.DeliveryTag, false);
        };
        
        // NotifyOrderPaymentSucceded.
        var orderPaymentSuccededConsumer = new EventingBasicConsumer(_model);
        orderPaymentSuccededConsumer.Received += (ch, ea) =>
        {
            Console.WriteLine("Notifications: NotifyOrderPaymentSucceded command received.");

            using (var transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var datetime = DateTime.UtcNow;
                var command = RabbitMqUtils.DeserealizeMessage<NotifyOrderPaymentSucceded>(ea);
            
                // Get recipient.
                var sql = _getRecipientSql(command.UserId);
                var recipient = _dbConnection.QueryFirst<Recipient>(sql);
            
                // Send message.
                var message = $"The order {command.OrderTitle} with price {command.OrderPrice} successfully payed!";
                sql = _insertNotificationMessageSql(recipient.UserId, recipient.Email, message, datetime);
                _dbConnection.Execute(sql);
                transactionScope.Complete();
            }
            
            _model.BasicAck(ea.DeliveryTag, false);

        };
        
        // NotifyOrderPaymentDeclined.
        var orderPaymentDeclinedConsumer = new EventingBasicConsumer(_model);
        orderPaymentDeclinedConsumer.Received += (ch, ea) =>
        {
            Console.WriteLine("Notifications: NotifyOrderPaymentDeclined command received.");

            using (var transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var datetime = DateTime.UtcNow;
                var command = RabbitMqUtils.DeserealizeMessage<NotifyOrderPaymentDeclined>(ea);
            
                // Get recipient.
                var sql = _getRecipientSql(command.UserId);
                var recipient = _dbConnection.QueryFirst<Recipient>(sql);
            
                // Send message.
                var message = $"The order {command.OrderTitle} with price {command.OrderPrice} declined!";
                sql = _insertNotificationMessageSql(recipient.UserId, recipient.Email, message, datetime);
                _dbConnection.Execute(sql);
                transactionScope.Complete();
            }
            
            _model.BasicAck(ea.DeliveryTag, false);
        };
        
        // NotifyOrderReturned.
        var orderReturnedConsumer = new EventingBasicConsumer(_model);
        orderReturnedConsumer.Received += (ch, ea) =>
        {
            Console.WriteLine("Notifications: NotifyOrderReturned command received.");

            using (var transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var datetime = DateTime.UtcNow;
                var command = RabbitMqUtils.DeserealizeMessage<NotifyOrderReturned>(ea);
            
                // Get recipient.
                var sql = _getRecipientSql(command.UserId);
                var recipient = _dbConnection.QueryFirst<Recipient>(sql);
            
                // Send message.
                var message = $"The order {command.OrderTitle} returned!";
                sql = _insertNotificationMessageSql(recipient.UserId, recipient.Email, message, datetime);
                _dbConnection.Execute(sql);
                transactionScope.Complete();
            }
            
            _model.BasicAck(ea.DeliveryTag, false);
        };
        
        // Apply consumers.
        _model.BasicConsume(RabbitMqUtils.GetQueueName(typeof(NotifyUserCreated), ServiceName.Notifications), false, userCreatedConsumer);
        _model.BasicConsume(RabbitMqUtils.GetQueueName(typeof(NotifyOrderCreated), ServiceName.Notifications), false, orderCreatedConsumer);
        _model.BasicConsume(RabbitMqUtils.GetQueueName(typeof(NotifyOrderPaymentSucceded), ServiceName.Notifications), false, orderPaymentSuccededConsumer);
        _model.BasicConsume(RabbitMqUtils.GetQueueName(typeof(NotifyOrderPaymentDeclined), ServiceName.Notifications), false, orderPaymentDeclinedConsumer);
        _model.BasicConsume(RabbitMqUtils.GetQueueName(typeof(NotifyOrderReturned), ServiceName.Notifications), false, orderReturnedConsumer);
    }

    private void Initialize()
    {
        RabbitMqUtils.InitializeServiceQueueForMessageType(_model, typeof(NotifyUserCreated), ServiceName.Notifications);
        RabbitMqUtils.InitializeServiceQueueForMessageType(_model, typeof(NotifyOrderCreated), ServiceName.Notifications);
        RabbitMqUtils.InitializeServiceQueueForMessageType(_model, typeof(NotifyOrderPaymentSucceded), ServiceName.Notifications);
        RabbitMqUtils.InitializeServiceQueueForMessageType(_model, typeof(NotifyOrderPaymentDeclined), ServiceName.Notifications);
        RabbitMqUtils.InitializeServiceQueueForMessageType(_model, typeof(NotifyOrderReturned), ServiceName.Notifications);
    }
}