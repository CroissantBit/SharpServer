namespace SharpServer.Types;

public class SongMetadata
{
    public string Name { get; }
    public string Bpm { get; }
    public string Duration { get; }
    public bool IsPrivate { get; }
    public string Password { get; }

    public SongMetadata(string name, string bpm, string duration, bool isPrivate, string password)
    {
        Name = name;
        Bpm = bpm;
        Duration = duration;
        IsPrivate = isPrivate;
        Password = password;
    }
}
