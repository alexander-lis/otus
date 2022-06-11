using System.ComponentModel.DataAnnotations;
using System.Transactions;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Common.Infrastructure;
using MyApp.Common.RabbitMq;
using MyApp.Orders.Models;
using MyApp.Park.Contract.Events;
using MySqlConnector;

namespace MyApp.Orders.Controllers;

[Route("")]
[ApiController]
[AllowAnonymous]
public class ScootersController : ControllerBase
{
    // Поля.
    private readonly MySqlConnection _connection;
    private readonly IRabbitMqService _rabbitMqService;

    // Скрипты.
    private readonly Func<string> _getScootersSql =
        () =>
            $"SELECT * FROM orders_scooters";
    
    private readonly Func<int, string> _getScooterSql =
        (id) =>
            $"SELECT * FROM orders_scooters WHERE id = {id}";

    // Метрики.
    private readonly MetricsCollector _metricsCollector;

    public ScootersController(MySqlConnection connection, IRabbitMqService rabbitMqService)
    {
        _connection = connection;
        _rabbitMqService = rabbitMqService;
        _metricsCollector = new MetricsCollector("MyApp.Scooters", "ScootersController");
    }

    [HttpGet]
    public async Task<ActionResult<ICollection<ScooterStatus>>> GetScooters(CancellationToken cancellationToken)
    {
        return await _metricsCollector.ExecuteWithMetrics("GetScooters", async () =>
        {
            var scooters = await _connection.QueryAsync<ScooterStatus>(_getScootersSql(), cancellationToken);
            return Ok(scooters);
        });
    }
    
    [HttpGet("{scooterStatusId}")]
    public async Task<ActionResult<ICollection<ScooterStatus>>> GetScooters(int scooterStatusId, CancellationToken cancellationToken)
    {
        return await _metricsCollector.ExecuteWithMetrics("GetScooter", async () =>
        {
            var scooters = await _connection.QueryAsync<ScooterStatus>(_getScooterSql(scooterStatusId), cancellationToken);
            return Ok(scooters);
        });
    }
}