using System.Diagnostics;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using MyApp.Backend.Infrastructure;
using MyApp.Backend.Models;
using MySqlConnector;
using Prometheus;

namespace MyApp.Backend.Controllers;

[Route("[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
    // Поля.
    private readonly MySqlConnection _connection;

    // Скрипты.
    private readonly Func<CreateUserDto, string> _insertUserSql =
        user => $"INSERT INTO users (name) VALUES ('{user.Name}')";

    private readonly Func<int, string> _selectUserSql = id => $"SELECT * FROM users WHERE id = {id}";
    private readonly string _selectUsersSql = "SELECT * FROM users";

    private readonly Func<UpdateUserDto, int, string> _updateUserSql =
        (user, id) => $"UPDATE users SET name = '{user.Name}' WHERE id = {id}";

    private readonly Func<int, string> _deleteUserSql = id => $"DELETE FROM users WHERE id = {id}";

    // Метрики.
    private readonly MetricsCollector _metricsCollector;


    public UsersController(MySqlConnection connection)
    {
        _connection = connection;
        _metricsCollector = new MetricsCollector("UsersController");
    }

    [HttpPost]
    public async Task<ActionResult> CreateUser(CreateUserDto user, CancellationToken cancellationToken)
    {
        return await _metricsCollector.ExecuteWithMetrics("CreateUser", async () =>
        {
            var rows = await _connection.ExecuteAsync(_insertUserSql(user), cancellationToken);

            if (rows == 0)
            {
                return Problem();
            }

            return Ok();
        });

    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> UpdateUser(int id, [FromBody] UpdateUserDto user,
        CancellationToken cancellationToken)
    {
        return await _metricsCollector.ExecuteWithMetrics("UpdateUser", async () =>
        {
            var rows = await _connection.ExecuteAsync(_updateUserSql(user, id), cancellationToken);

            if (rows == 0)
            {
                return NotFound();
            }

            return Ok();
        });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserDto>> ReadUser(int id, CancellationToken cancellationToken)
    {
        return await _metricsCollector.ExecuteWithMetrics("ReadUser", async () =>
        {
            var user = await _connection.QuerySingleOrDefaultAsync<UserDto>(_selectUserSql(id), cancellationToken);

            if (user is null)
            {
                return NotFound();
            }

            return Ok(user);
        });
    }

    [HttpGet]
    public async Task<ActionResult<ICollection<UserDto>>> ReadUsers(CancellationToken cancellationToken)
    {
        return await _metricsCollector.ExecuteWithMetrics("ReadUsers", async () =>
        {
            var users = await _connection.QueryAsync<UserDto>(_selectUsersSql, cancellationToken);
            return Ok(users);
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteUser(int id, CancellationToken cancellationToken)
    {
        return await _metricsCollector.ExecuteWithMetrics("DeleteUser", async () =>
        {
            var rows = await _connection.ExecuteAsync(_deleteUserSql(id), cancellationToken);

            if (rows == 0)
            {
                return NotFound();
            }

            return Ok();
        });
    }
}