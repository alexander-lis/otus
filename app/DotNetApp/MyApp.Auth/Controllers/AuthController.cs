using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MyApp.Auth.Models;
using MyApp.Common.Authentication;
using MyApp.Common.Infrastructure;
using MySqlConnector;

namespace MyApp.Auth.Controllers;

[Route("[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    // Поля.
    private readonly MySqlConnection _connection;

    // Скрипты.
    // Скрипты.
    private readonly Func<RegisterUserDto, string> _insertUserSql =
        user =>
            $"INSERT INTO auth_users (login, password) VALUES ('{user.Login}', '{user.Password}')";

    private readonly Func<string, string, string> _selectUserSql =
        (login, password) =>
            $"SELECT * FROM auth_users WHERE login = '{login}' and password = '{password}'";

    // Метрики.
    private readonly MetricsCollector _metricsCollector;


    public AuthController(MySqlConnection connection)
    {
        _connection = connection;
        _metricsCollector = new MetricsCollector("MyApp.Auth", "AuthController");
    }

    [HttpPost("register")]
    public async Task<ActionResult> Register(RegisterUserDto user, CancellationToken cancellationToken)
    {
        return await _metricsCollector.ExecuteWithMetrics("Register", async () =>
        {
            var rows = await _connection.ExecuteAsync(_insertUserSql(user), cancellationToken);

            if (rows == 0)
            {
                return Problem();
            }

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