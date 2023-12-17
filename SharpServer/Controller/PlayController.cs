using System.Text.RegularExpressions;
using DotNetEnv;
using SharpServer.Database;
using SharpServer.Remote;
using SharpServer.Servers;

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
        var songId = _httpContext.Request.RouteValues["id"].ToString();
        Console.WriteLine(songId);
        var regex = new Regex("^[0-9]+$");
        if (!regex.IsMatch(songId))
        {
            _httpContext.Response.StatusCode = 403;
            return "Nah nah nah";
        }

        var list = DatabaseClient
            .GetDatabase()
            .Query<Types.Video>("select * from songs where id = " + songId + ";");
        var path = Env.GetString("CACHE_DIR") + "/Mp4Files";
        Console.WriteLine(path);
        var filenames = Directory.GetFiles(path);
        var songIsOnLocalSystem = false;

        var filenameToSearch = list[0].Name + ".mp4";
        Console.WriteLine("file to search " + filenameToSearch);
        foreach (var songName in filenames)
        {
            Console.WriteLine("on e of entrues-" + songName);
            if (songName.Contains(filenameToSearch))
            {
                Console.WriteLine("found");
                _httpContext.Response.StatusCode = 200;
                return list[0].Name;
            }
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
            Console.WriteLine("Download complete");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            _httpContext.Response.StatusCode = 503;
            throw new Exception("Unable to downlaod files");
        }

        _httpContext.Response.StatusCode = 200;
        return list[0].Name;
    }
}
