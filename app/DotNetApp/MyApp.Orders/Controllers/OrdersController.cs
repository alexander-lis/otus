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
            $"INSERT INTO orders_orders (userid, title, price, scooterid) VALUES ('{order.UserId}', '{order.Title}', '{order.Price}', '{order.ScooterStatusId}'); SELECT LAST_INSERT_ID()";

    private readonly Func<int, int, string> _updateScooterStatus =
        (id, status) =>
            $"UPDATE orders_scooters SET status = {status} WHERE id = {id}";
    
    private readonly Func<string, DateTime, string> _insertIdempotencyKeySql =
        (id, validto) =>
            $"INSERT INTO orders_idempotency (id, validto) VALUES ('{id}', '{validto.ToString("yyyy-MM-dd H:mm:ss")}'); SELECT LAST_INSERT_ID()";

    private readonly Func<string, DateTime, string> _getIdempotencyKeySql =
        (key, now) =>
            $"SELECT id FROM orders_idempotency WHERE id = '{key}'";
    
    private readonly Func<int, string> _getOrderSql =
        (id) =>
            $"SELECT * FROM orders_orders WHERE id = '{id}'";
    
    private readonly Func<int, string> _getScooterStatusSql =
        (id) =>
            $"SELECT * FROM orders_scooters WHERE id = '{id}'";

    // Метрики.
    private readonly MetricsCollector _metricsCollector;

    public OrdersController(MySqlConnection connection, IRabbitMqService rabbitMqService)
    {
        _connection = connection;
        _rabbitMqService = rabbitMqService;
        _metricsCollector = new MetricsCollector("MyApp.Orders", "OrdersController");
    }
    
    [HttpPost]
    public async Task<ActionResult> CreateOrder(CreateOrderDto order, CancellationToken cancellationToken)
    {
        return await _metricsCollector.ExecuteWithMetrics("CreateOrder", async () =>
        {
            using var transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

                 
            var scooterStatus = await _connection.QueryFirstOrDefaultAsync<ScooterStatus>(_getScooterStatusSql(order.ScooterStatusId));
            if (scooterStatus is null)
            {
                return NotFound();
            }

            await _connection.ExecuteAsync(_updateScooterStatus(scooterStatus.Id, 2));

            var id = await _connection.ExecuteScalarAsync<int>(_insertOrderSql(order), cancellationToken);

            _rabbitMqService.PublishEvent(new OrderCreated(order.UserId, id, order.Title, order.Price));
            _rabbitMqService.SendCommand(new NotifyOrderCreated(order.UserId, id, order.Title, order.Price));
            
            transactionScope.Complete();
            
            return Ok();
        });
    }

    [HttpPost("{orderId}/return")]
    public async Task<ActionResult> ReturnOrder(int id, CancellationToken cancellationToken)
    {
        return await _metricsCollector.ExecuteWithMetrics("ReturnOrder", async () =>
        {
            using var transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            var order = await _connection.QueryFirstOrDefaultAsync<Order>(_getOrderSql(id));
            if (order is null)
            {
                return NotFound();
            }
            
            await _connection.ExecuteAsync(_updateScooterStatus(order.ScooterStatusId, 1), cancellationToken);
            _rabbitMqService.SendCommand(new NotifyOrderReturned(order.UserId, order.Id, order.Title));

            transactionScope.Complete();

            return Ok();
        });
    }
}   