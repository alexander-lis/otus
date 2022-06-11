using MyApp.Common.Configuration;
using MyApp.Orders.BackgroundServices;

// Create builder.
var builder = WebApplication.CreateBuilder(args);
Configurator.ConfigurateBuilder(builder);

// Add services.
builder.Services.AddHostedService<RabbitMqListener>();

// Build app.
var app = builder.Build();
Configurator.ConfigurateApp(app);

app.Run();