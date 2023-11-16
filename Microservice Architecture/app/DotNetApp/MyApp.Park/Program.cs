using MyApp.Common.Configuration;

// Create builder.
var builder = WebApplication.CreateBuilder(args);
Configurator.ConfigurateBuilder(builder);

// Build app.
var app = builder.Build();
Configurator.ConfigurateApp(app);

app.Run();