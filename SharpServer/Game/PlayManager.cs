using System.Text.RegularExpressions;
using SharpServer.Database;
using SharpServer.Upload;

namespace SharpServer.Game;

public class PlayManager
{
    private readonly HttpContext _httpContext;

    public PlayManager(HttpContext context)
    {
        _httpContext = context;
    }

    public async Task<string> Handle()
    {
        var regex = new Regex("^[0-9]+$");
        var songId = _httpContext.Request.RouteValues["id"]?.ToString();
        if (songId == null)
        {
            _httpContext.Response.StatusCode = 400;
            throw new Exception("No song id provided");
        }

        if (!regex.IsMatch(songId))
        {
            _httpContext.Response.StatusCode = 403;
            throw new Exception("nah nah nah");
        }

        var list = DatabaseClient
            .GetDatabase()
            .Query<Types.Song>("select * from songs where id = " + songId + ";");
        var path = "./bin/Mp4Files";
        var filenames = Directory.GetFiles(path);
        var filenameToSearch = list[0].SongName + ".mp4";
        foreach (var songName in filenames)
            if (songName.Contains(filenameToSearch))
            {
                _httpContext.Response.StatusCode = 200;
                return list[0].SongName;
            }

        Console.WriteLine("Starting download");
        var mp4Name = list[0].SongName + ".mp4";
        var mp3Name = list[0].SongName + ".wav";
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
            throw new Exception("Unable to download files");
        }

        _httpContext.Response.StatusCode = 200;
        return list[0].SongName;
    }
}
