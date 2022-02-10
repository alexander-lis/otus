using Microsoft.Extensions.Configuration;
using MySqlConnector;

// Конфиг.
var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.Development.json")
    .AddEnvironmentVariables()
    .Build();

var connectionString = config.GetValue<string>("ConnectionStrings:Default");
var dbName = config.GetValue<string>("DbName");

// Коннекшн.


// Создание БД.
using var connDb = new MySqlConnection(connectionString);
connDb.Open();

using var createDbCommand = connDb.CreateCommand();
createDbCommand.CommandText = $"CREATE DATABASE IF NOT EXISTS `${dbName}`;";
createDbCommand.ExecuteNonQuery();

connDb.Close();

// Создание таблицы.
using var connTable = new MySqlConnection($"{connectionString};database={dbName}");
connTable.Open();

using var createTableCommand = connTable.CreateCommand();
createTableCommand.CommandText = @"create table users
    (id INT NOT NULL AUTO_INCREMENT,    
    name VARCHAR(100) NOT NULL,
    PRIMARY KEY (id)    );";
createTableCommand.ExecuteNonQuery();

connTable.Close();

