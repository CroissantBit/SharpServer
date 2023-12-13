using System.Diagnostics.CodeAnalysis;
using MySql.Data.MySqlClient;
using Serilog;

namespace SharpServer.Database;

public class DatabaseClient
{
    [MaybeNull]
    private static DatabaseClient _instance;
    private readonly MySqlConnection _mySqlConnection;

    public DatabaseClient()
    {
        _mySqlConnection = new MySqlConnection(
            Environment.GetEnvironmentVariable("DATABASE_CONNECTION_URL")
        );
        try
        {
            _mySqlConnection.Open();
            Log.Information("Connected to database");
        }
        catch (Exception e)
        {
            throw new Exception("Couldn't connect to the database", e);
        }
    }

    public static DatabaseClient GetDatabase()
    {
        if (_instance != null)
            return _instance;
        try
        {
            _instance = new DatabaseClient();
        }
        catch (Exception e)
        {
            throw new Exception("Couldn't create the database client", e);
        }

        return _instance;
    }

    public List<T> Query<T>(string command)
        where T : new()
    {
        var list = new List<T>();

        using (var cmd = new MySqlCommand(command, _mySqlConnection))
        {
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var row = new string[reader.FieldCount];
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        row[i] = reader[i].ToString();
                    }

                    var element = new T();
                    var d = element as DatabaseTable;
                    list.Add((T)d.InstantiateObject(row));
                }
            }
        }

        return list;
    }

    public List<T> Insert<T>(string command, String[] data)
        where T : new()
    {
        var list = new List<T>();
        command = fillCommandStr(command, data);
        using (var cmd = new MySqlCommand(command, _mySqlConnection))
        {
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var row = new string[reader.FieldCount];
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        row[i] = reader[i].ToString();
                    }

                    var element = new T();
                    var d = element as DatabaseTable;
                    list.Add((T)d.InstantiateObject(row));
                }
            }
        }

        return list;
    }

    public bool CheckIfRecordExist(string command, string[] data)
    {
        command = fillCommandStr(command, data);
        using (var cmd = new MySqlCommand(command, _mySqlConnection))
        {
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    return true;
                }
            }
        }

        return false;
    }

    private string fillCommandStr(string command, IReadOnlyList<string> data)
    {
        var counter = data.Count - 1;
        var index = command.Length - 1;
        while (command.Contains('?') && counter >= 0)
        {
            index = command.LastIndexOf('?');
            command = command.Remove(index, 1);
            command = command.Insert(index, data[counter]);
            counter--;
        }

        return command.Replace("?", "");
    }
}
