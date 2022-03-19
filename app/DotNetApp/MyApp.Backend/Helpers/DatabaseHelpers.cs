using MySqlConnector;

namespace MyApp.Backend.Helpers;

public static class DatabaseHelpers
{
    
    public static T ExecuteReader<T>(Func<MySqlDataReader, T> convert, MySqlConnection connection, string script)
    {
        connection.Open();
        
        var cmd = connection.CreateCommand();
        cmd.CommandText = script;
        var reader = cmd.ExecuteReader();

        var result = convert(reader);

        connection.Close();

        return result;
    }

    public static T ExecuteNonQuery<T>(Func<MySqlCommand, T> process, MySqlConnection connection, string script)
    {
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = script;
        cmd.ExecuteNonQuery();

        var result = process(cmd);

        connection.Close();

        return result;
    }
    
    public static void ExecuteNonQuery(MySqlConnection connection, string script)
    {
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = script;
        cmd.ExecuteNonQuery();

        connection.Close();
    }

    public static bool IsAny(MySqlDataReader rdr)
    {
        while (rdr.Read())
        {
            return true;
        }

        return false;
    }
}