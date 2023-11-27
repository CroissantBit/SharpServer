﻿using System.Net;
using FfmpegWrapper;
using SharpServer.Database;
using SharpServer.FfmpegWrapper;
using SharpServer.Server;
using SharpServer.Upload;

namespace SharpServer;

public class UploadController
{
    private HttpContext _httpContext;

    public UploadController(HttpContext context)
    {
        _httpContext = context;
    }

    public async Task HandleUpload()
    {
        var form = await _httpContext.Request.ReadFormAsync();
        var songName = form["songName"];
        var isPrivate = form["private"] == "true";
        var bpm = form["bpm"];

        if (songName == "" || songName == string.Empty)
        {
            _httpContext.Response.StatusCode = 400;
            throw new Exception("No song name provided");
        }

        if (
            DatabaseClient
                .GetDatabase()
                .CheckIfRecordExist(
                    "select * from songs where name = '?';",
                    new string[] { songName }
                )
        )
        {
            _httpContext.Response.StatusCode = (int)HttpStatusCode.UnprocessableEntity;
            throw new Exception("Song with this name already exists");
        }

        var file = form.Files.GetFile("file");
        if (file == null)
        {
            _httpContext.Response.StatusCode = 418;
            throw new Exception("No file received");
        }

        if (file.ContentType != "video/mp4")
        {
            _httpContext.Response.StatusCode = (int)HttpStatusCode.UnprocessableEntity;
            throw new Exception("Invalid file format. Only video/mp4 is allowed.");
        }

        string password = "";
        if (isPrivate)
        {
            if (form["password"] == "" || form["password"] == string.Empty)
            {
                _httpContext.Response.StatusCode = 400;
                throw new Exception("To make a song private, you need to provide a password");
            }

            password = form["password"];
        }

        //save file to disc
        var filePathMp4 = "./bin/Mp4Files/" + songName + ".mp4";

        await using (var stream = new FileStream(filePathMp4, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        String duration = FFmpegWrapper.GetFFmpegWrapper().GetSongDuration(songName);

        var filePathWav = $"./bin/WavFiles/{songName}.wav";
        FFmpegWrapper.GetFFmpegWrapper().CreateWavFile(songName, filePathWav);

        string mp4ObjectName = CreateObjectName(songName, ".mp4");
        string wavObjectName = CreateObjectName(songName, ".wav");

        var taskMp4Upload = FileServer
            .GetFileServer()
            .UploadFileAsync(mp4ObjectName, filePathMp4, "video/mp4");
        var taskWavUpload = FileServer
            .GetFileServer()
            .UploadFileAsync(wavObjectName, filePathWav, "audio/wav");

        try
        {
            DatabaseClient
                .GetDatabase()
                .Insert<Types.Song>(
                    "INSERT INTO songs (name, duration, bpm, isPrivate, password ) VALUES ('?', '?', ?, ?, '?');",
                    new string[] { songName, duration, bpm, isPrivate.ToString(), password }
                );
        }
        catch (Exception e)
        {
            _httpContext.Response.StatusCode = 503;
            throw new Exception("Unable to save file to database");
        }
        Task.WaitAll(taskMp4Upload, taskWavUpload);
    }

    private string CreateObjectName(string songName, string dataType)
    {
        return songName + dataType;
    }
}