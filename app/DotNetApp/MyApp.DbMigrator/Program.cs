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
drop table if exists notification_history;
drop table if exists notification_recipients;
drop table if exists orders_orders;
drop table if exists orders_idempotency;";
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
    PRIMARY KEY (id));
";
createTableCommand1.ExecuteNonQuery();

// Создание таблиц Billing.
using var createTableCommand2 = connTable.CreateCommand();
createTableCommand2.CommandText = @"create table if not exists billing_accounts
    (userid INT NOT NULL,
    money INT NOT NULL,
    PRIMARY KEY (userid));
";
createTableCommand2.ExecuteNonQuery();

// Создание таблиц Notifications.
using var createTableCommand3 = connTable.CreateCommand();
createTableCommand3.CommandText = @"
create table if not exists notification_history
    (id INT NOT NULL AUTO_INCREMENT,    
    userid INT NOT NULL,
    email VARCHAR(100) NOT NULL,
    message VARCHAR(1000) NOT NULL,
    datetime DATETIME NOT NULL,
    PRIMARY KEY (id));
    
create table if not exists notification_recipients
    (userid INT NOT NULL,
    email VARCHAR(100) NOT NULL,
    PRIMARY KEY (userid));
";
createTableCommand3.ExecuteNonQuery();

// Создание таблиц Orders.
using var createTableCommand4 = connTable.CreateCommand();
createTableCommand4.CommandText = @"
create table if not exists orders_orders
    (id INT NOT NULL AUTO_INCREMENT,    
    userid INT NOT NULL,
    title VARCHAR(100) NOT NULL,
    price int NOT NULL,
    PRIMARY KEY (id));

create table if not exists orders_idempotency
    (id VARCHAR(36) NOT NULL,
    validto DATETIME NOT NULL,
    PRIMARY KEY (id));
";
createTableCommand4.ExecuteNonQuery();

//

Console.WriteLine("CREATING TABLES - FINISH");

connTable.Close();