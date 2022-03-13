using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MyApp.Backend.Models;
using MySqlConnector;
using Prometheus;

namespace MyApp.Backend.Controllers;

[Route("[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
    private MySqlConnection _connection;

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

        _connection.Open();
        var cmd = ExecuteNonQuery(_connection, $"INSERT INTO users (name) VALUES ('{user.Name}')");
        _connection.Close();

        var id = cmd.LastInsertedId;

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
        _connection.Open();
        if (!IsAny(ExecuteReader(_connection, $"SELECT * FROM users WHERE id = {id}")))
        {
            return NotFound();
        }

        _connection.Close();

        // Обновление.
        _connection.Open();
        ExecuteNonQuery(_connection, $"UPDATE users SET name = '{user.Name}' WHERE id = {id}");
        _connection.Close();

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

        _connection.Open();
        using var rdr = ExecuteReader(_connection, $"SELECT * FROM users WHERE id = {id}");

        UserDto? user = null;
        while (rdr.Read())
        {
            user = new UserDto()
            {
                Id = rdr.GetInt32(0),
                Name = rdr.GetString(1)
            };
        }

        _connection.Close();

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

        _connection.Open();
        using var rdr = ExecuteReader(_connection, $"SELECT * FROM users");

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

        _connection.Close();

        CollectElapsedTime("ReadUsers", stopwatch);
        
        return users;
    }

    [HttpDelete("{id:int}")]
    public ActionResult DeleteUser(int id)
    {
        // Проверка.
        _connection.Open();
        if (!IsAny(ExecuteReader(_connection, $"SELECT * FROM users WHERE id = {id}")))
        {
            return NotFound();
        }

        _connection.Close();

        // Удаление.
        _connection.Open();
        ExecuteNonQuery(_connection, $"DELETE FROM users WHERE id = {id}");
        _connection.Close();
        return Ok();
    }

    private MySqlCommand ExecuteNonQuery(MySqlConnection connection, string script)
    {
        var cmd = _connection.CreateCommand();
        cmd.CommandText = script;
        cmd.ExecuteNonQuery();
        return cmd;
    }

    private MySqlDataReader ExecuteReader(MySqlConnection connection, string script)
    {
        var cmd = _connection.CreateCommand();
        cmd.CommandText = script;
        var reader = cmd.ExecuteReader();
        return reader;
    }

    private bool IsAny(MySqlDataReader rdr)
    {
        while (rdr.Read())
        {
            return true;
        }

        return false;
    }
    
    private void CollectElapsedTime(
        string methodName,
        Stopwatch stopwatch)
    {
        stopwatch.Stop();
        _summary.Labels("UsersController", methodName).Observe(stopwatch.ElapsedMilliseconds);
    }
}