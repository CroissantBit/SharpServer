using System.Text.RegularExpressions;
using DotNetEnv;
using Serilog;
using SharpServer.Database;
using SharpServer.Remote;

namespace SharpServer.Controller;

public class PlayController
{
    private readonly HttpContext _httpContext;

    public PlayController(HttpContext context)
    {
        _httpContext = context;
    }

    public async Task<string> Handle()
    {
        var songId = _httpContext.Request.RouteValues["id"]?.ToString();
        Console.WriteLine(songId);
        var regex = new Regex("^[0-9]+$");
        if (songId != null && !regex.IsMatch(songId))
        {
            _httpContext.Response.StatusCode = 403;
            return "Nah nah nah";
        }

        var list = DatabaseClient
            .GetDatabase()
            .Query<Types.Video>("select * from songs where id = " + songId + ";");
        var path = Env.GetString("CACHE_DIR") + "/Mp4Files";
        Console.WriteLine(path);
        Console.WriteLine("got here");
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        var filenames = Directory.GetFiles(path);
        Console.WriteLine("not here");
        var filenameToSearch = list[0].Name + ".mp4";
        Console.WriteLine("file to search " + filenameToSearch);
        foreach (var songName in filenames)
        {
            Console.WriteLine("on e of entrues-" + songName);
            if (!songName.Contains(filenameToSearch))
                continue;
            Console.WriteLine("found");
            _httpContext.Response.StatusCode = 200;
            return list[0].Name;
        }

        var mp4Name = list[0].Name + ".mp4";
        var mp3Name = list[0].Name + ".wav";
        try
        {
            var taskMp4Download = FileServer
                .GetFileServer()
                .DownloadFileAsync(mp4Name, Env.GetString("CACHE_DIR") + "/Mp4Files");
            var taskMp3Download = FileServer
                .GetFileServer()
                .DownloadFileAsync(mp3Name, Env.GetString("CACHE_DIR") + "/Mp3Files");
            Task.WaitAll(taskMp4Download, taskMp3Download);
            Log.Information("Download complete");
        }
        catch (Exception e)
        {
            Log.Error(e.Message);
            _httpContext.Response.StatusCode = 503;
            throw new Exception("Unable to download files");
        }

        _httpContext.Response.StatusCode = 200;
        return list[0].Name;
    }
}
