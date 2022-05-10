using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Transactions;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MyApp.Auth.Contract.Events;
using MyApp.Auth.Models;
using MyApp.Common.Authentication;
using MyApp.Common.Configuration;
using MyApp.Common.Infrastructure;
using MyApp.Common.RabbitMq;
using MyApp.Notifications.Contract.Commands;
using MySqlConnector;

namespace MyApp.Auth.Controllers;

[Route("")]
[ApiController]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    // Поля.
    private readonly MySqlConnection _connection;
    private readonly IRabbitMqService _rabbitMqService;

    // Скрипты.
    private readonly Func<RegisterUserDto, string> _insertUserSql =
        user =>
            $"INSERT INTO auth_users (login, password, name, email) VALUES ('{user.Login}', '{user.Password}', '{user.Name}', '{user.Email}'); SELECT LAST_INSERT_ID()";

    private readonly Func<string, string, string> _selectUserSql =
        (login, password) =>
            $"SELECT * FROM auth_users WHERE login = '{login}' and password = '{password}'";

    // Метрики.
    private readonly MetricsCollector _metricsCollector;


    public AuthController(MySqlConnection connection, IRabbitMqService rabbitMqService)
    {
        _connection = connection;
        _rabbitMqService = rabbitMqService;
        _metricsCollector = new MetricsCollector("MyApp.Auth", "AuthController");
    }

    [HttpPost("register")]
    public async Task<ActionResult> Register(RegisterUserDto user, CancellationToken cancellationToken)
    {
        return await _metricsCollector.ExecuteWithMetrics("Register", async () =>
        {
            using var transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            
            var id = await _connection.ExecuteScalarAsync<int>(_insertUserSql(user), cancellationToken);
        
            _rabbitMqService.PublishEvent(new UserCreated(id, user.Login));
            _rabbitMqService.PublishCommand(new NotifyUserCreated(id, user.Login));
            
            transactionScope.Complete();
            
            return Ok();
        });
    }

    [HttpPost("token")]
    public async Task<ActionResult> Token(string login, string password, CancellationToken cancellationToken)
    {
        return await _metricsCollector.ExecuteWithMetrics("Token", async () =>
        {
            var identity = await GetIdentity(login, password, cancellationToken);

            if (identity is null)
            {
                return BadRequest(new { errorText = "Неверное имя пользователя или пароль" });
            }

            var utcNow = DateTime.UtcNow;
            var jwt = new JwtSecurityToken(
                issuer: AuthOptions.ISSUER,
                audience: AuthOptions.AUDIENCE,
                claims: identity.Claims,
                expires: utcNow.Add(TimeSpan.FromMinutes(AuthOptions.LIFETIME)),
                signingCredentials: new SigningCredentials(AuthOptions.GetSymmetricSecurityKey(),
                    SecurityAlgorithms.HmacSha256));

            var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);

            return Ok(encodedJwt);
        });
    }

    private async Task<ClaimsIdentity?> GetIdentity(string login, string password, CancellationToken cancellationToken)
    {
        var user = await _connection.QuerySingleOrDefaultAsync<UserDto>(
            _selectUserSql(login, password),
            cancellationToken);

        if (user is null)
        {
            return null;
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimsIdentity.DefaultNameClaimType, user.Login),
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        };
        ClaimsIdentity claimsIdentity =
            new ClaimsIdentity(
                claims, "Token",
                ClaimsIdentity.DefaultNameClaimType,
                ClaimsIdentity.DefaultRoleClaimType);
        return claimsIdentity;
    }
}