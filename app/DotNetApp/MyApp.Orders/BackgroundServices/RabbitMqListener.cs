using Dapper;
using MyApp.Common.RabbitMq;
using MyApp.Notifications.Contract.Events;
using MyApp.Orders.Contract.Events;
using MyApp.Park.Contract.Events;
using MySqlConnector;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MyApp.Orders.BackgroundServices;

public class RabbitMqListener : BackgroundService
{
    private readonly IModel _model;
    private readonly MySqlConnection _dbConnection;
    private readonly IRabbitMqService _rabbitMqService;
    
    // Скрипты.
    private readonly Func<int, string> _getDeleteOrderSql =
        (orderId) =>
            $"DELETE FROM orders_orders WHERE id = {orderId}";
    
    private readonly Func<ScooterCreated, string> _insertScooterStatusSql =
        (scooter) =>
            $"INSERT INTO orders_scooters (scooter_id, name, status) VALUES ({scooter.Id}, '{scooter.Name}', 1); SELECT LAST_INSERT_ID()";
    
    private readonly Func<int, string> _deleteScooterSql =
        (scooterid) =>
            $"UPDATE orders_scooters SET status = 3 WHERE scooterid = {scooterid}";
    
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
        // OrderPaymentDeclined.
        var orderPaymentDeclinedConsumer = new EventingBasicConsumer(model);
        orderPaymentDeclinedConsumer.Received += (ch, ea) =>
        {
            Console.WriteLine("Orders: OrderPaymentDeclined event received.");
            var order = RabbitMqUtils.DeserealizeMessage<OrderPaymentDeclined>(ea);

            var sql = _getDeleteOrderSql(order.OrderId);
            _dbConnection.ExecuteAsync(sql);

            model.BasicAck(ea.DeliveryTag, false);
        };
        
        // ScooterCreated.
        var scooterCreated = new EventingBasicConsumer(model);
        scooterCreated.Received += (ch, ea) =>
        {
            Console.WriteLine("Oreders: ScooterCreated event received.");
            var created = RabbitMqUtils.DeserealizeMessage<ScooterCreated>(ea);

            var sql = _insertScooterStatusSql(created);
            _dbConnection.ExecuteAsync(sql);

            model.BasicAck(ea.DeliveryTag, false);
        };

        // ScooterDeleted.
        var scooterDeleted = new EventingBasicConsumer(model);
        scooterDeleted.Received += (ch, ea) =>
        {
            Console.WriteLine("Oreders: ScooterCreated event received.");
            var deleted = RabbitMqUtils.DeserealizeMessage<ScooterDeleted>(ea);

            var sql = _deleteScooterSql(deleted.Id);
            _dbConnection.ExecuteAsync(sql);

            model.BasicAck(ea.DeliveryTag, false);
        };

        model.BasicConsume(RabbitMqUtils.GetQueueName(typeof(OrderPaymentDeclined), ServiceName.Orders), false, orderPaymentDeclinedConsumer);
        model.BasicConsume(RabbitMqUtils.GetQueueName(typeof(ScooterCreated), ServiceName.Orders), false, scooterCreated);
        model.BasicConsume(RabbitMqUtils.GetQueueName(typeof(ScooterDeleted), ServiceName.Orders), false, scooterDeleted);
    }
    
    private void Initialize()
    {
        RabbitMqUtils.InitializeServiceQueueForMessageType(_model, typeof(OrderPaymentDeclined), ServiceName.Orders);
        RabbitMqUtils.InitializeServiceQueueForMessageType(_model, typeof(ScooterCreated), ServiceName.Orders);
        RabbitMqUtils.InitializeServiceQueueForMessageType(_model, typeof(ScooterDeleted), ServiceName.Orders);
    }
}