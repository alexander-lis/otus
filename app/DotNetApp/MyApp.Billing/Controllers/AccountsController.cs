using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Billing.Models;
using MyApp.Common.Infrastructure;
using MyApp.Common.RabbitMq;
using MySqlConnector;

namespace MyApp.Billing.Controllers;

[Route("")]
[ApiController]
[AllowAnonymous]
public class AccountsController : ControllerBase
{
    // Поля.
    private readonly MySqlConnection _connection;
    private readonly IRabbitMqService _rabbitMqService;

    // Скрипты.
    private readonly Func<int, string> _selectAccountSql =
        userid => $"SELECT * FROM billing_accounts WHERE userid = {userid}";

    // Метрики.
    private readonly MetricsCollector _metricsCollector;

    public AccountsController(MySqlConnection connection, IRabbitMqService rabbitMqService)
    {
        _connection = connection;
        _rabbitMqService = rabbitMqService;
        _metricsCollector = new MetricsCollector("MyApp.Billing", "AccountsController");
    }

    [HttpGet("{userId}")]
    public async Task<ActionResult<BillingAccount>> GetByUserId(int userId, CancellationToken cancellationToken)
    {
        return await _metricsCollector.ExecuteWithMetrics("GetByUserId", async () =>
        {
            var notifications = await _connection.QueryFirstOrDefaultAsync<BillingAccount>(_selectAccountSql(userId), cancellationToken);
            return Ok(notifications);
        });
    }
}