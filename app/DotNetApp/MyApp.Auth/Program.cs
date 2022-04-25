using MySqlConnector;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = $"{builder.Configuration["ConnectionString"]};database={builder.Configuration["DbName"]}";
builder.Services.AddTransient(_ => new MySqlConnection(connectionString));

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

// Prometheus
app.UseMetricServer();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();