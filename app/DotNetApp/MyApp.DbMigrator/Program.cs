using Microsoft.Extensions.Configuration;
using MySqlConnector;

// Конфиг.
var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.Development.json")
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

Console.WriteLine("DELETING TABLES - START");
// Таблицы Auth.
using var deleteTableCommand1 = connTable.CreateCommand();
deleteTableCommand1.CommandText = @"drop table if exists auth_users";
deleteTableCommand1.ExecuteNonQuery();

// Таблицы Backend.
using var deleteTableCommand2 = connTable.CreateCommand();
deleteTableCommand2.CommandText = @"drop table if exists back_users";
deleteTableCommand2.ExecuteNonQuery();
Console.WriteLine("DELETING TABLES - FINISH");

Console.WriteLine("CREATING TABLES - START");
// Таблицы Auth.
using var createTableCommand1 = connTable.CreateCommand();
createTableCommand1.CommandText = @"create table if not exists auth_users
    (id INT NOT NULL AUTO_INCREMENT,    
    login VARCHAR(100) NOT NULL,
    password VARCHAR(100) NOT NULL,
    PRIMARY KEY (id));";
createTableCommand1.ExecuteNonQuery();

// Таблицы Backend.
using var createTableCommand2 = connTable.CreateCommand();
createTableCommand2.CommandText = @"create table if not exists back_users
    (id INT NOT NULL AUTO_INCREMENT,    
    login VARCHAR(100) NOT NULL,
    PRIMARY KEY (id));";
createTableCommand2.ExecuteNonQuery();
Console.WriteLine("CREATING TABLES - FINISH");

connTable.Close();

