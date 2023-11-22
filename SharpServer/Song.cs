using System.Dynamic;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;

namespace FFMpegWrapper;

public class Song : DataBaseTable
{
    public int ID { get; }
    public string Songname { get; }
    public string Duration { get; }
    public int BPM { get; set; }

    public bool IsPrivate { get; }

    public string Password { get; }

    public Song() { }

    public Song(int id, String songname, String duration, int bpm, bool isPrivate, String password)
    {
        ID = id;
        Songname = songname;
        Duration = duration;
        BPM = bpm;
        IsPrivate = isPrivate;
        Password = password;
    }

    public override object instantiateObject(string[] args)
    {
        var id = Convert.ToInt32(args[0]);
        var songname = args[1];
        var duration = args[2];
        var bpm = Convert.ToInt32(args[3]);
        bool isPrivate = args[4] == "true";
        var password = args[5];
        return new Song(id, songname, duration, bpm, isPrivate, password);
    }
}
