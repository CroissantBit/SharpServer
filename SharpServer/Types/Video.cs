using Croissantbit;
using SharpServer.Database;

namespace SharpServer.Types;

public class Video : DatabaseTable
{
    public int Id { get; }
    public string Name { get; }
    public string Duration { get; }
    public int Bpm { get; set; }
    public bool IsPrivate { get; }
    public string Password { get; }

    public Video(int id, string name, string duration, int bpm, bool isPrivate, string password)
    {
        Id = id;
        Name = name;
        Duration = duration;
        Bpm = bpm;
        IsPrivate = isPrivate;
        Password = password;
    }

    public Video() { }

    public override object InstantiateObject(string[] args)
    {
        var id = Convert.ToInt32(args[0]);
        var songName = args[1];
        var duration = args[2];
        var bpm = Convert.ToInt32(args[3]);
        var isPrivate = args[4] == "true";
        var password = args[5];
        return new Video(id, songName, duration, bpm, isPrivate, password);
    }

    public VideoMetadata toVideoMetadata()
    {
        return new VideoMetadata
        {
            Bitrate = 0,
            Bpm = Bpm,
            Duration = int.Parse(Duration),
            Id = Id,
            Processing = false,
            Size = 0,
            Title = Name
        };
    }
}
