namespace FFMpegWrapper.Properties;

public class MetadataSong
{
    public string Name { get; }
    public string Bpm { get; }
    public string Duration { get; }
    public bool IsPrivate { get; }
    public string Password { get; }

    public MetadataSong(string name, string bpm, string duration, bool isPrivate, string? password)
    {
        Name = name;
        Bpm = Bpm;
        Duration = duration;
        IsPrivate = isPrivate;
        Password = password;
    }
}
