using MySql.Data.MySqlClient;

namespace FFMpegWrapper;

public class Database
{
    private static Database _instance;
    private MySqlConnection _mySqlConnection;

    public Database()
    {
        string connectionString = "Server=sql11.freesqldatabase.com;Database=sql11661704;User=sql11661704;Password=bcsu95BFXm;";
        _mySqlConnection = new MySqlConnection(connectionString);
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

    public static Database GetDatabase()
    {
        if (_instance == null)
        {
            try
            {
                _instance = new Database();
            }
            catch (Exception e)
            {
                throw new Exception("Couldnt create databse");
            }
        }
        return _instance;
    }

    public List<T> Query<T>(string command) where T : new() 
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
                    var d = element as DataBaseTable;
                    list.Add((T)d.instantiateObject(row));
                }
            }
        }

        return list;
    }
    
    public List<T> Insert<T>(string command,String[] data) where T : new() 
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
                    var d = element as DataBaseTable;
                    list.Add((T)d.instantiateObject(row));
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
            command = command.Insert(index , data[counter]);
            counter--;
        }
        return command.Replace("?", "");
    }
}