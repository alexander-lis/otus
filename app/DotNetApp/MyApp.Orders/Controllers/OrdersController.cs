using System.Transactions;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
            $"INSERT INTO orders_orders (userid, title, price, scooterstatusid) VALUES ('{order.UserId}', '{order.Title}', '{order.Price}', '{order.ScooterStatusId}'); SELECT LAST_INSERT_ID()";

    private readonly Func<int, string> _getOrderSql =
        (id) =>
            $"SELECT * FROM orders_orders WHERE id = '{id}'";
    
    private readonly Func<int, int, string> _updateScooterStatusSql =
        (id, status) =>
            $"UPDATE orders_scooters SET status = {status} WHERE id = {id}";
    
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
    public async Task<ActionResult<int>> CreateOrder(CreateOrderDto order, CancellationToken cancellationToken)
    {
        return await _metricsCollector.ExecuteWithMetrics("CreateOrder", async () =>
        {
            int id = -1;
            using (var transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var scooterStatus = await _connection.QueryFirstOrDefaultAsync<ScooterStatus>(_getScooterStatusSql(order.ScooterStatusId));
                if (scooterStatus is null)
                {
                    return NotFound();
                }

                await _connection.ExecuteAsync(_updateScooterStatusSql(scooterStatus.Id, 2));

                id = await _connection.ExecuteScalarAsync<int>(_insertOrderSql(order), cancellationToken);

                transactionScope.Complete();
            }
         
            _rabbitMqService.PublishEvent(new OrderCreated(order.UserId, id, order.Title, order.Price));
            _rabbitMqService.SendCommand(new NotifyOrderCreated(order.UserId, id, order.Title, order.Price));
            
            return Ok(id);
        });
    }

    [HttpPost("{orderId}/return")]
    public async Task<ActionResult> ReturnOrder(int orderId, CancellationToken cancellationToken)
    {
        return await _metricsCollector.ExecuteWithMetrics("ReturnOrder", async () =>
        {
            Order order;
            using (var transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                order = await _connection.QueryFirstOrDefaultAsync<Order>(_getOrderSql(orderId));
                if (order is null)
                {
                    return NotFound();
                }
            
                await _connection.ExecuteAsync(_updateScooterStatusSql(order.ScooterStatusId, 1), cancellationToken);
                transactionScope.Complete();
            }

            _rabbitMqService.SendCommand(new NotifyOrderReturned(order.UserId, order.Id, order.Title));

            return Ok();
        });
    }
}   