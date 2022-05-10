using MyApp.Common.Configuration;

var builder = WebApplication.CreateBuilder(args);
Configurator.ConfigurateBuilder(builder);

var app = builder.Build();
Configurator.ConfigurateApp(app);

app.Run();