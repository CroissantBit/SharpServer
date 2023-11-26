using System.Diagnostics.CodeAnalysis;
using MySql.Data.MySqlClient;

namespace SharpServer.Database;

public class DatabaseClient
{
    [MaybeNull]
    private static DatabaseClient _instance;
    private MySqlConnection _mySqlConnection;

    public DatabaseClient()
    {
        _mySqlConnection = new MySqlConnection(
            Environment.GetEnvironmentVariable("DATABASE_CONNECTION_URL")
        );
        try
        {
            _mySqlConnection.Open();
            Console.WriteLine("worked");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
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
        List<T> list = new List<T>();

        using (MySqlCommand cmd = new MySqlCommand(command, _mySqlConnection))
        {
            using (MySqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    string[] row = new string[reader.FieldCount];
                    for (int i = 0; i < reader.FieldCount; i++)
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
        List<T> list = new List<T>();
        command = fillCommandStr(command, data);
        using (MySqlCommand cmd = new MySqlCommand(command, _mySqlConnection))
        {
            using (MySqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    string[] row = new string[reader.FieldCount];
                    for (int i = 0; i < reader.FieldCount; i++)
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

    public bool CheckIfRecordExist(String command, String[] data)
    {
        command = fillCommandStr(command, data);
        using (MySqlCommand cmd = new MySqlCommand(command, _mySqlConnection))
        {
            using (MySqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    return true;
                }
            }
        }

        return false;
    }

    private string fillCommandStr(String command, String[] data)
    {
        int counter = data.Length - 1;
        int index = command.Length - 1;
        while (command.Contains("?") && counter >= 0)
        {
            index = command.LastIndexOf('?');
            command = command.Remove(index, 1);
            command = command.Insert(index, data[counter]);
            counter--;
        }

        return command.Replace("?", "");
    }
}
