using System.Net;
using FFMpegWrapper.Properties;
using Serilog;
using Serilog.Events;

namespace FFMpegWrapper;

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
            bool isPrivate = form["private"] == "true";
            var bpm = form["bpm"];
            
            
        
            if (Database.GetDatabase()
                .CheckIfRecordExist("select * from songs where name = '?';", new String[] { songName }))
            {
                _httpContext.Response.StatusCode = (int)HttpStatusCode.UnprocessableEntity;
                throw new Exception("Songname already picked");
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
                if (form["password"] == "" || form["password"] == String.Empty)
                {
                    _httpContext.Response.StatusCode = 400;
                    throw new Exception("If you wanna make the song private, please add a password.");
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
            FFmpegWrapper.GetFFmpegWrapper().CreateWavFile(songName,filePathWav);
            


            string mp4ObjectName = createObjectName(songName, ".mp4");
            string wavObjectName = createObjectName(songName, ".wav");

            var taskMp4Uplaod = FileServer.GetFileServer().UploadFileAsync(mp4ObjectName, filePathMp4, DataTypes.Mp4);
            var taskWavUplaod = FileServer.GetFileServer().UploadFileAsync(wavObjectName, filePathWav, DataTypes.Wav);
            
            try
            {
                Database.GetDatabase().Insert<Song>(
                    "INSERT INTO songs (name, duration, bpm, isPrivate, password ) VALUES ('?', '?', ?, ?, '?');"
                    , new String[] { songName, duration, bpm, isPrivate.ToString(), password});
            }
            catch (Exception e)
            {
                _httpContext.Response.StatusCode = 503;
                throw new Exception("Unable to save file to database");
            }
            Task.WaitAll(taskMp4Uplaod, taskWavUplaod);
    }

    public String createObjectName(string songName, string dataType)
    {
        return songName + dataType;
    }
}