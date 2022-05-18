using System.Transactions;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Auth.Contract.Events;
using MyApp.Common.Infrastructure;
using MyApp.Common.RabbitMq;
using MyApp.Notifications.Contract.Commands;
using MyApp.Orders.Contract.Events;
using MyApp.Orders.Models;
using MySqlConnector;

namespace MyApp.Orders.Controllers;

[Route("")]
[ApiController]
[AllowAnonymous]
public class OrdersController : ControllerBase
{
    // Поля.
    private readonly MySqlConnection _connection;
    private readonly IRabbitMqService _rabbitMqService;

    // Скрипты.
    private readonly Func<CreateOrderDto, string> _insertOrderSql =
        (order) =>
            $"INSERT INTO orders_orders (userid, title, price) VALUES ('{order.UserId}', '{order.Title}', '{order.Price}'); SELECT LAST_INSERT_ID()";

    // Метрики.
    private readonly MetricsCollector _metricsCollector;

    public OrdersController(MySqlConnection connection, IRabbitMqService rabbitMqService)
    {
        _connection = connection;
        _rabbitMqService = rabbitMqService;
        _metricsCollector = new MetricsCollector("MyApp.Orders", "OrdersController");
    }
    
    [HttpPost]
    public async Task<ActionResult> Register(CreateOrderDto order, CancellationToken cancellationToken)
    {
        return await _metricsCollector.ExecuteWithMetrics("Register", async () =>
        {
            using var transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            
            var id = await _connection.ExecuteScalarAsync<int>(_insertOrderSql(order), cancellationToken);
        
            _rabbitMqService.PublishEvent(new OrderCreated(order.UserId, id, order.Title, order.Price));
            _rabbitMqService.PublishCommand(new NotifyOrderCreated(order.UserId, id, order.Title, order.Price));
            
            transactionScope.Complete();
            
            return Ok();
        });
    }
}