using System.ComponentModel.DataAnnotations;
using System.Transactions;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Common.Infrastructure;
using MyApp.Common.RabbitMq;
using MyApp.Park.Contract.Events;
using MyApp.Park.Models;
using MySqlConnector;

namespace MyApp.Park.Controllers;

[Route("")]
[ApiController]
[AllowAnonymous]
public class ScootersController : ControllerBase
{
    // Поля.
    private readonly MySqlConnection _connection;
    private readonly IRabbitMqService _rabbitMqService;

    // Скрипты.
    private readonly Func<CreateScooterDto, string> _insertScooterSql =
        (scooter) =>
            $"INSERT INTO park_scooters (name) VALUES ('{scooter.Name}'); SELECT LAST_INSERT_ID()";

    private readonly Func<string> _getScootersSql =
        () =>
            $"SELECT * FROM park_scooters";

    private readonly Func<int, string> _deleteScooterSql =
        (scooterId) =>
            $"DELETE FROM park_scooters WHERE id = {scooterId}";

    // Метрики.
    private readonly MetricsCollector _metricsCollector;

    public ScootersController(MySqlConnection connection, IRabbitMqService rabbitMqService)
    {
        _connection = connection;
        _rabbitMqService = rabbitMqService;
        _metricsCollector = new MetricsCollector("MyApp.Park", "ScootersController");
    }

    [HttpPost]
    public async Task<ActionResult> CreateScooter(CreateScooterDto scooter, CancellationToken cancellationToken)
    {
        return await _metricsCollector.ExecuteWithMetrics("CreateScooter", async () =>
        {
            var id = -1;
            using (var transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                id = await _connection.ExecuteScalarAsync<int>(_insertScooterSql(scooter), cancellationToken);

                transactionScope.Complete();
            }

            _rabbitMqService.PublishEvent(new ScooterCreated(id, scooter.Name));

            return Ok();
        });
    }
    
    [HttpDelete]
    public async Task<ActionResult> DeleteScooter(int id, CancellationToken cancellationToken)
    {
        return await _metricsCollector.ExecuteWithMetrics("CreateScooter", async () =>
        {
            using (var transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                await _connection.ExecuteScalarAsync<int>(_deleteScooterSql(id), cancellationToken);
                transactionScope.Complete();
            }

            _rabbitMqService.PublishEvent(new ScooterDeleted(id));

            return Ok();
        });
    }
    
    [HttpGet]
    public async Task<ActionResult<ICollection<Scooter>>> GetScooters(CancellationToken cancellationToken)
    {
        return await _metricsCollector.ExecuteWithMetrics("GetScooters", async () =>
        {
            var scooters = await _connection.QueryAsync<Scooter>(_getScootersSql(), cancellationToken);
            return Ok(scooters);
        });
    }
}