using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyApp.Common.Authentication;
using MyApp.Common.RabbitMq;
using MySqlConnector;
using Prometheus;
using RabbitMQ.Client;

namespace MyApp.Common.Configuration;

public static class Configurator
{
    private const string DB_CONFIG_SECTION = "DbConnectionString";
    private const string RABBITMQ_CONFIG_SECTION = "RmqConnectionString";

    public static void ConfigurateBuilder(WebApplicationBuilder builder)
    {
        // Add services to the container.
        builder.Services.AddControllers();
        builder.Services.AddScoped<IRabbitMqService, RabbitMqService>();

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

        // Configuration.
        if (builder.Environment.IsDevelopment())
        {
            var folder = Path.Combine(builder.Environment.ContentRootPath, "..", "MyApp.Common", "Configuration");
            var file = Path.Combine(folder, "appsettings.Development.json");
            builder.Configuration.AddJsonFile(file);
        }

        // DB config.
        var dbConnectionString =
            $"{builder.Configuration[DB_CONFIG_SECTION]};database={builder.Configuration["DbName"]}";
        builder.Services.AddTransient(_ => new MySqlConnection(dbConnectionString));

        // RabbitMQ config.
        var rmqConnectionString = builder.Configuration[Configurator.RABBITMQ_CONFIG_SECTION];
        builder.Services.AddSingleton<IConnectionFactory, ConnectionFactory>(_ => new ConnectionFactory()
            { Uri = new Uri(rmqConnectionString) });
    }

    public static void ConfigurateApp(WebApplication app)
    {
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

        // Prometheus.
        app.UseMetricServer();

        app.UseHttpsRedirection();

        app.UseAuthentication();

        app.UseAuthorization();

        app.MapControllers();
    }
}