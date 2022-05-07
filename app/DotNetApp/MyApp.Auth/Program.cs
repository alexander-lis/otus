using Microsoft.AspNetCore.Authentication.JwtBearer;
using MyApp.Common.Authentication;
using MySqlConnector;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Authentication.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = AuthOptions.TokenValidationParameters;
    });

var connectionString = $"{builder.Configuration["ConnectionString"]};database={builder.Configuration["DbName"]}";
builder.Services.AddTransient(_ => new MySqlConnection(connectionString));

var app = builder.Build();

// Configure base path.
var pathBase = Environment.GetEnvironmentVariable("ASPNETCORE_APPL_PATH");

if (!string.IsNullOrWhiteSpace(pathBase))
{
    app.UsePathBase($"/{pathBase.TrimStart('/')}");
    Console.WriteLine($"Using PathBase: {pathBase}");
}

app.UseRouting();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

// Prometheus
app.UseMetricServer();

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();