using Dapper;
using MyApp.Auth.Contract.Events;
using MyApp.Billing.Models;
using MyApp.Common.RabbitMq;
using MyApp.Notifications.Contract.Commands;
using MyApp.Notifications.Contract.Events;
using MyApp.Orders.Contract.Events;
using MySqlConnector;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MyApp.Billing.BackgroundServices;

public class RabbitMqListener : BackgroundService
{
    private readonly IModel _model;
    private readonly MySqlConnection _dbConnection;
    private readonly IRabbitMqService _rabbitMqService;
    
    private readonly Func<int, int, string> _insertBillingAccountSql =
        (userId, money) =>
            $"INSERT INTO billing_accounts (userid, money) VALUES ('{userId}', '{money}');";
    
    private readonly Func<int, string> _getBillingAccountSql =
        (userId) =>
            $"SELECT * FROM billing_accounts WHERE userId = {userId}";
    
    private readonly Func<int, int, string> _updateBillingAccountSql =
        (userId, money) =>
            $"UPDATE billing_accounts SET money = {money} WHERE userid = {userId}";
    
    public RabbitMqListener(IConnectionFactory connectionFactory, MySqlConnection dbConnection, IRabbitMqService rabbitMqService)
    {
        var connection = connectionFactory.CreateConnection();
        _model = connection.CreateModel();
        _dbConnection = dbConnection;
        _rabbitMqService = rabbitMqService;

        Initialize();
    }

    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ConsumeEvents(_model);
        return Task.CompletedTask;
    }

    private void ConsumeEvents(IModel model)
    {
        // UserCreated.
        var userCreatedConsumer = new EventingBasicConsumer(model);
        userCreatedConsumer.Received += (ch, ea) =>
        {
            Console.WriteLine("Billing: UserCreated event received.");
            var content = RabbitMqUtils.DeserealizeMessage<UserCreated>(ea);
            var sql = _insertBillingAccountSql(content.Id, 1000);
            _dbConnection.Execute(sql);
            Console.WriteLine("Billing: UserCreated event creates billing account.");
            model.BasicAck(ea.DeliveryTag, false);
        };
        
        // OrderCreated.
        var orderCreatedConsumer = new EventingBasicConsumer(model);
        orderCreatedConsumer.Received += (ch, ea) =>
        {
            Console.WriteLine("Billing: OrderCreated event received.");
            var order = RabbitMqUtils.DeserealizeMessage<OrderCreated>(ea);

            var sql = _getBillingAccountSql(order.UserId);
            var acc = _dbConnection.QueryFirst<BillingAccount>(sql);

            if (acc.Money >= order.OrderPrice)
            {
                var sql2 = _updateBillingAccountSql(order.UserId, acc.Money - order.OrderPrice);
                _dbConnection.Execute(sql2);
                Console.WriteLine("Billing: OrderCreated event publishes NotifyOrderPaymentSucceded.");
                RabbitMqService.SendCommand(_model, new NotifyOrderPaymentSucceded(order.UserId, order.OrderId, order.OrderTitle, order.OrderPrice));
            }
            else
            {
                Console.WriteLine("Billing: OrderCreated event publishes NotifyOrderPaymentDeclined.");
                RabbitMqService.SendCommand(_model, new NotifyOrderPaymentDeclined(order.UserId, order.OrderId, order.OrderTitle, order.OrderPrice));
                RabbitMqService.PublishEvent(_model, new OrderPaymentDeclined(order.OrderId));
            }
            
            
            model.BasicAck(ea.DeliveryTag, false);
        };

        model.BasicConsume(RabbitMqUtils.GetQueueName(typeof(UserCreated), ServiceName.Billing), false, userCreatedConsumer);
        model.BasicConsume(RabbitMqUtils.GetQueueName(typeof(OrderCreated), ServiceName.Billing), false, orderCreatedConsumer);
    }
    
    private void Initialize()
    {
        RabbitMqUtils.InitializeServiceQueueForMessageType(_model, typeof(UserCreated), ServiceName.Billing);
        RabbitMqUtils.InitializeServiceQueueForMessageType(_model, typeof(OrderCreated), ServiceName.Billing);
    }
}