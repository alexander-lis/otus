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

using var connTable = new MySqlConnection($"{connectionString};database={dbName}");
connTable.Open();

// Удаление таблиц.
Console.WriteLine("DELETING TABLES - START");
using var deleteTableCommand1 = connTable.CreateCommand();
deleteTableCommand1.CommandText = @"
drop table if exists auth_users;
drop table if exists billing_accounts;
drop table if exists notification_log;";
deleteTableCommand1.ExecuteNonQuery();
Console.WriteLine("DELETING TABLES - FINISH");

Console.WriteLine("CREATING TABLES - START");

// Создание таблиц Auth.
using var createTableCommand1 = connTable.CreateCommand();
createTableCommand1.CommandText = @"create table if not exists auth_users
    (id INT NOT NULL AUTO_INCREMENT,    
    login VARCHAR(100) NOT NULL,
    password VARCHAR(100) NOT NULL,
    email VARCHAR(100) NOT NULL,
    name VARCHAR(100) NOT NULL,
    PRIMARY KEY (id));";
createTableCommand1.ExecuteNonQuery();

// Создание таблиц Billing.
using var createTableCommand2 = connTable.CreateCommand();
createTableCommand2.CommandText = @"create table if not exists billing_accounts
    (id INT NOT NULL AUTO_INCREMENT,    
    user_id INT NOT NULL,
    money INT NOT NULL,
    PRIMARY KEY (id));";
createTableCommand2.ExecuteNonQuery();

// Создание таблиц Notifications.
using var createTableCommand3 = connTable.CreateCommand();
createTableCommand3.CommandText = @"create table if not exists notification_log
    (id INT NOT NULL AUTO_INCREMENT,    
    user_id INT NOT NULL,
    email VARCHAR(100) NOT NULL,
    datetime DATETIME NOT NULL,
    PRIMARY KEY (id));";
createTableCommand3.ExecuteNonQuery();

Console.WriteLine("CREATING TABLES - FINISH");

connTable.Close();