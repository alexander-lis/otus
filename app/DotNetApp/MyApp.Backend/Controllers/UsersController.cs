using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MyApp.Backend.Helpers;
using MyApp.Backend.Models;
using MySqlConnector;
using Prometheus;

namespace MyApp.Backend.Controllers;

[Route("[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly MySqlConnection _connection;

    // Скрипты.
    private readonly Func<CreateUserDto, string> _insertUserSql = user => $"INSERT INTO users (name) VALUES ('{user.Name}')";
    private readonly Func<int, string> _selectUserSql = id => $"SELECT * FROM users WHERE id = {id}";
    private readonly string _selectUsersSql = "SELECT * FROM users";

    private readonly Func<UpdateUserDto, int, string> _updateUserSql =
        (user, id) => $"UPDATE users SET name = '{user.Name}' WHERE id = {id}";

    private readonly Func<int, string> _deleteUserSql = id => $"DELETE FROM users WHERE id = {id}";

    private readonly Summary _summary = Metrics.CreateSummary(
        "elapsed_time_ms_summary",
        "Records latency of methods.",
        new SummaryConfiguration()
        {
            AgeBuckets = 5,
            BufferSize = 500,
            MaxAge = new TimeSpan(0, 0, 2, 0),
            LabelNames = new[] { "class_name", "method_name" },
            Objectives = new List<QuantileEpsilonPair>
            {
                new QuantileEpsilonPair(0.5, 0.05),
                new QuantileEpsilonPair(0.95, 0.005),
                new QuantileEpsilonPair(0.99, 0.001),
            }
        });

    public UsersController(MySqlConnection connection)
    {
        _connection = connection;
    }

    [HttpPost]
    public ActionResult<UserDto> CreateUser(CreateUserDto user)
    {
        var stopwatch = Stopwatch.StartNew();

        var id = DatabaseHelpers.ExecuteNonQuery(cmd => cmd.LastInsertedId, _connection, _insertUserSql(user));

        CollectElapsedTime("CreateUser", stopwatch);

        return Ok(new UserDto()
        {
            Id = (int)id,
            Name = user.Name
        });
    }

    [HttpPut("{id:int}")]
    public ActionResult<UserDto> UpdateUser(int id, [FromBody] UpdateUserDto user)
    {
        // Проверка.
        if (!DatabaseHelpers.ExecuteReader(DatabaseHelpers.IsAny, _connection, _selectUserSql(id)))
        {
            return NotFound();
        }

        // Обновление.
        DatabaseHelpers.ExecuteNonQuery(_connection, _updateUserSql(user, id));

        return Ok(new UserDto()
        {
            Id = id,
            Name = user.Name
        });
    }

    [HttpGet("{id:int}")]
    public ActionResult<UserDto> ReadUser(int id)
    {
        var stopwatch = Stopwatch.StartNew();

        var user = DatabaseHelpers.ExecuteReader(rdr =>
        {
            UserDto? user = null;
            while (rdr.Read())
            {
                user = new UserDto()
                {
                    Id = rdr.GetInt32(0),
                    Name = rdr.GetString(1)
                };
            }

            return user;
        }, _connection, _selectUserSql(id));

        if (user is null)
        {
            return NotFound();
        }

        CollectElapsedTime("ReadUser", stopwatch);

        return Ok(user);
    }

    [HttpGet]
    public ActionResult<ICollection<UserDto>> ReadUsers()
    {
        var stopwatch = Stopwatch.StartNew();

        var users = DatabaseHelpers.ExecuteReader(rdr =>
        {
            var users = new List<UserDto>();

            while (rdr.Read())
            {
                var user = new UserDto()
                {
                    Id = rdr.GetInt32(0),
                    Name = rdr.GetString(1)
                };
                users.Add(user);
            }

            return users;
        }, _connection, _selectUsersSql);


        CollectElapsedTime("ReadUsers", stopwatch);

        return users;
    }

    [HttpDelete("{id:int}")]
    public ActionResult DeleteUser(int id)
    {
        // Проверка.
        if (!DatabaseHelpers.ExecuteReader(DatabaseHelpers.IsAny, _connection, _selectUserSql(id)))
        {
            return NotFound();
        }

        // Удаление.
        DatabaseHelpers.ExecuteNonQuery(_connection, _deleteUserSql(id));

        return Ok();
    }

    private void CollectElapsedTime(
        string methodName,
        Stopwatch stopwatch)
    {
        stopwatch.Stop();
        _summary.Labels("UsersController", methodName).Observe(stopwatch.ElapsedMilliseconds);
    }
}