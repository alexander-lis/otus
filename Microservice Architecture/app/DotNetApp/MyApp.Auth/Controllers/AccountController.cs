using System.Security.Claims;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Auth.Models;
using MyApp.Common.Infrastructure;
using MySqlConnector;

namespace MyApp.Auth.Controllers;

[Route("[controller]")]
[ApiController]
public class AccountController : ControllerBase
{
    // TODO: удалить пользователей отсюда.
    // Поля.
    private readonly MySqlConnection _connection;

    // Скрипты.
    private readonly Func<int, string> _selectUserSql = id => $"SELECT * FROM auth_users WHERE id = {id}";

    private readonly Func<UpdateUserDto, int, string> _updateUserSql =
        (user, id) => $"UPDATE auth_users SET name = '{user.Name}' WHERE id = {id}";

    // Метрики.
    private readonly MetricsCollector _metricsCollector;

    public AccountController(MySqlConnection connection)
    {
        _connection = connection;
        _metricsCollector = new MetricsCollector("MyApp.Backend", "UsersController");
    }

    [HttpPut]
    [Authorize]
    public async Task<ActionResult> UpdateCurrentUser([FromBody] UpdateUserDto user,
        CancellationToken cancellationToken)
    {
        return await _metricsCollector.ExecuteWithMetrics("UpdateCurrentUser", async () =>
        {
            var userSubClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userSubClaim?.Value, out var userId))
            {
                return Problem();
            }
            
            var rows = await _connection.ExecuteAsync(_updateUserSql(user, userId), cancellationToken);

            if (rows == 0)
            {
                return NotFound();
            }

            return Ok();
        });
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<UserDto>> ReadCurrentUser(CancellationToken cancellationToken)
    {
        return await _metricsCollector.ExecuteWithMetrics("ReadCurrentUser", async () =>
        {
            var userSubClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userSubClaim?.Value, out var userId))
            {
                return Problem();
            }
            
            var user = await _connection.QuerySingleOrDefaultAsync<UserDto>(_selectUserSql(userId), cancellationToken);

            if (user is null)
            {
                return Problem();
            }

            return Ok(user);
        });
    }
}