using SharpServer.Database;

namespace SharpServer.Types;

public class Song : DatabaseTable
{
    public int Id { get; }
    public string SongName { get; }
    public string Duration { get; }
    public int Bpm { get; set; }
    public bool IsPrivate { get; }
    public string Password { get; }

    public Song() { }

    public Song(int id, string songName, string duration, int bpm, bool isPrivate, string password)
    {
        Id = id;
        SongName = songName;
        Duration = duration;
        Bpm = bpm;
        IsPrivate = isPrivate;
        Password = password;
    }

    public override object InstantiateObject(string[] args)
    {
        var id = Convert.ToInt32(args[0]);
        var songName = args[1];
        var duration = args[2];
        var bpm = Convert.ToInt32(args[3]);
        bool isPrivate = args[4] == "true";
        var password = args[5];
        return new Song(id, songName, duration, bpm, isPrivate, password);
    }
}
