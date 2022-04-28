using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Backend.Models;
using MyApp.Common.Infrastructure;
using MySqlConnector;

namespace MyApp.Backend.Controllers;

[Route("[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
    // TODO: удалить пользователей отсюда.
    // Поля.
    private readonly MySqlConnection _connection;

    // Скрипты.
    private readonly Func<CreateUserDto, string> _insertUserSql =
        user => $"INSERT INTO back_users (login) VALUES ('{user.Login}')";

    private readonly Func<int, string> _selectUserSql = id => $"SELECT * FROM back_users WHERE id = {id}";
    private readonly string _selectUsersSql = "SELECT * FROM back_users";

    private readonly Func<UpdateUserDto, int, string> _updateUserSql =
        (user, id) => $"UPDATE back_users SET login = '{user.Login}' WHERE id = {id}";

    private readonly Func<int, string> _deleteUserSql = id => $"DELETE FROM back_users WHERE id = {id}";

    // Метрики.
    private readonly MetricsCollector _metricsCollector;


    public UsersController(MySqlConnection connection)
    {
        _connection = connection;
        _metricsCollector = new MetricsCollector("MyApp.Backend", "UsersController");
    }

    [HttpPost]
    [Authorize]
    [Obsolete("Здесь не должно быть создания")]
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
    [Authorize]
    [Obsolete("Здесь не должно быть обновления")]
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
    [Authorize]
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
    [Authorize]
    public async Task<ActionResult<ICollection<UserDto>>> ReadUsers(CancellationToken cancellationToken)
    {
        return await _metricsCollector.ExecuteWithMetrics("ReadUsers", async () =>
        {
            var users = await _connection.QueryAsync<UserDto>(_selectUsersSql, cancellationToken);
            return Ok(users);
        });
    }

    [HttpDelete("{id:int}")]
    [Authorize]
    [Obsolete("Здесь не должно быть удаления")]
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