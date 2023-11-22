using System.Text.RegularExpressions;
using FFMpegWrapper;

namespace SharpServer;

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
        var regex = new Regex("^[0-9]+$");
        if (!regex.IsMatch(songId))
        {
            _httpContext.Response.StatusCode = 403;
            throw new Exception("nah nah nah");
        }

        var list = Database
            .GetDatabase()
            .Query<Song>("select * from songs where id = " + songId + ";");
        var path = "./bin/Mp4Files";
        var filenames = Directory.GetFiles(path);
        var songIsOnLocalSystem = false;
        var filenameToSearch = list[0].Songname + ".mp4";
        foreach (var songName in filenames)
            if (songName.Contains(filenameToSearch))
            {
                _httpContext.Response.StatusCode = 200;
                return list[0].Songname;
            }

        Console.WriteLine("start downlaoding");
        var mp4Name = list[0].Songname + ".mp4";
        var mp3Name = list[0].Songname + ".wav";
        try
        {
            var taskMp4Download = FileServer
                .GetFileServer()
                .DownloadFileAsync(mp4Name, "./bin/Mp4Files");
            var taskMp3Download = FileServer
                .GetFileServer()
                .DownloadFileAsync(mp3Name, "./bin/WavFiles");
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
        return list[0].Songname;
    }
}
