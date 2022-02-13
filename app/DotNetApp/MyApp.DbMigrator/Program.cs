using Microsoft.Extensions.Configuration;
using MySqlConnector;

// Конфиг.
var config = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .Build();

var connectionString = config.GetValue<string>("ConnectionString");
var dbName = config.GetValue<string>("DbName");

Console.WriteLine($"ConnectionString: {connectionString}, DbName: {dbName}");

// Создание БД.
using var connDb = new MySqlConnection(connectionString);
connDb.Open();

Console.WriteLine("CREATING DB - START");
using var createDbCommand = connDb.CreateCommand();
createDbCommand.CommandText = $"CREATE DATABASE IF NOT EXISTS `{dbName}`;";
createDbCommand.ExecuteNonQuery();
Console.WriteLine("CREATING DB - FINISH");

connDb.Close();

// Создание таблицы.
using var connTable = new MySqlConnection($"{connectionString};database={dbName}");
connTable.Open();

Console.WriteLine("CREATING TABLES - START");
using var createTableCommand = connTable.CreateCommand();
createTableCommand.CommandText = @"create table users
    (id INT NOT NULL AUTO_INCREMENT,    
    name VARCHAR(100) NOT NULL,
    PRIMARY KEY (id)    );";
createTableCommand.ExecuteNonQuery();
Console.WriteLine("CREATING TABLES - FINISH");

connTable.Close();

