using System.Transactions;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Common.Infrastructure;
using MyApp.Common.RabbitMq;
using MyApp.Notifications.Contract.Commands;
using MyApp.Notifications.Models;
using MySqlConnector;

namespace MyApp.Notifications.Controllers;

[Route("")]
[ApiController]
[AllowAnonymous]
public class RecipientsController : ControllerBase
{
    // Поля.
    private readonly MySqlConnection _connection;
    private readonly IRabbitMqService _rabbitMqService;

    // Скрипты.
    private readonly Func<int, string> _selectNotificationsSql =
        userid => $"SELECT * FROM notification_history WHERE userid = {userid}";

    // Метрики.
    private readonly MetricsCollector _metricsCollector;

    public RecipientsController(MySqlConnection connection, IRabbitMqService rabbitMqService)
    {
        _connection = connection;
        _rabbitMqService = rabbitMqService;
        _metricsCollector = new MetricsCollector("MyApp.Notifications", "RecipientsController");
    }

    [HttpGet("{userId}/notifications")]
    public async Task<ActionResult<ICollection<Notification>>> GetByUserId(int userId, CancellationToken cancellationToken)
    {
        return await _metricsCollector.ExecuteWithMetrics("Create", async () =>
        {
            var notifications = await _connection.QueryAsync<Notification>(_selectNotificationsSql(userId), cancellationToken);
            return Ok(notifications);
        });
    }
}