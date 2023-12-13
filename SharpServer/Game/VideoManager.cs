using System.Text.RegularExpressions;
using Serilog;
using SharpServer.Database;
using SharpServer.Upload;

namespace SharpServer.Game;

public static class VideoManager
{
    public static Types.Video GetVideo(int videoId)
    {
        var list = DatabaseClient
            .GetDatabase()
            .Query<Types.Video>("select * from songs where id = " + videoId + ";");
        if (list.Count == 0)
            throw new Exception("Video not found");

        var path = "./bin/Mp4Files";
        var filenames = Directory.GetFiles(path);
        var filenameToSearch = list[0].Name + ".mp4";
        if (filenames.Any(songName => songName.Contains(filenameToSearch)))
            return list[0];

        Log.Information($"Starting download of video {list[0].Name}");
        var mp4Name = list[0].Name + ".mp4";
        var mp3Name = list[0].Name + ".wav";
        var cacheDir = Environment.GetEnvironmentVariable("CACHE_DIR");
        Directory.CreateDirectory(cacheDir + "/Mp4Files");
        Directory.CreateDirectory(cacheDir + "/Mp3Files");
        try
        {
            var taskMp4Download = FileServer
                .GetFileServer()
                .DownloadFileAsync(mp4Name, cacheDir + "/Mp4Files");
            var taskMp3Download = FileServer
                .GetFileServer()
                .DownloadFileAsync(mp3Name, cacheDir + "/Mp3Files");
            Task.WaitAll(taskMp4Download, taskMp3Download);
            Log.Information("Download complete");
        }
        catch (Exception e)
        {
            Log.Error(e.Message);
            throw new Exception("Unable to download files");
        }

        return list[0];
    }
}
