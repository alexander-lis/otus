using System.ComponentModel.DataAnnotations;
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

    private readonly Func<string, DateTime, string> _insertIdempotencyKeySql =
        (id, validto) =>
            $"INSERT INTO orders_idempotency (id, validto) VALUES ('{id}', '{validto.ToString("yyyy-MM-dd H:mm:ss")}'); SELECT LAST_INSERT_ID()";

    private readonly Func<string, DateTime, string> _getIdempotencyKeySql =
        (key, now) =>
            $"SELECT id FROM orders_idempotency WHERE id = '{key}'";

    // Метрики.
    private readonly MetricsCollector _metricsCollector;

    public OrdersController(MySqlConnection connection, IRabbitMqService rabbitMqService)
    {
        _connection = connection;
        _rabbitMqService = rabbitMqService;
        _metricsCollector = new MetricsCollector("MyApp.Orders", "OrdersController");
    }
    
    [HttpPost]
    public async Task<ActionResult> CreateOrder([FromHeader(Name = "Idempotency-Key"), Required] Guid idempotencyKey, CreateOrderDto order, CancellationToken cancellationToken)
    {
        return await _metricsCollector.ExecuteWithMetrics("CreateOrder", async () =>
        {
            using var transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            var key = await _connection.QueryFirstOrDefaultAsync<string>(_getIdempotencyKeySql(idempotencyKey.ToString(), DateTime.UtcNow));

            if (key != null)
            {
                    return Problem("Повторный запрос!");
            }
            
            var id = await _connection.ExecuteScalarAsync<int>(_insertOrderSql(order), cancellationToken);

            var keyString = idempotencyKey.ToString();
            await _connection.ExecuteScalarAsync<string>(_insertIdempotencyKeySql(keyString.ToString(), DateTime.UtcNow.AddDays(1)));

            _rabbitMqService.PublishEvent(new OrderCreated(order.UserId, id, order.Title, order.Price));
            _rabbitMqService.PublishCommand(new NotifyOrderCreated(order.UserId, id, order.Title, order.Price));
            
            transactionScope.Complete();
            
            return Ok();
        });
    }
}   