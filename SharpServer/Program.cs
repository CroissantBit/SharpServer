using System.Reflection;
using System.Text.RegularExpressions;
using Amazon.S3;
using FFMpegWrapper;
using Serilog;
using SharpServer;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

DotNetEnv.Env.Load();

Log.Logger = new LoggerConfiguration()
    .MinimumLevel
    .Debug()
    .WriteTo
    .File($"Logs/{Assembly.GetExecutingAssembly().GetName().Name}.log")
    .CreateLogger();

FFmpegConfig.SetFFmpegConfig("./bin/ffmpeg.exe", false, true);

app.MapGet(
    "/",
    async (HttpContext context) =>
    {
        //testing
        /*
         SongManager songManager = new SongManager("huberts");
         Task o = songManager.playAudio();
         Task c = FFmpegWrapper.GetFFmpegWrapper()
             .customCommandTest(" -i  ./aaa.mp4 -s 140x80 -pix_fmt rgba -f image2pipe -vcodec png -");
         Task.WaitAll(c, o);
         */



        try
        {
            string htmlContent = await File.ReadAllTextAsync("index.html");
            context.Response.ContentType = "text/html";
            context.Response.StatusCode = 200;
            context.Response.WriteAsync(htmlContent);
        }
        catch (Exception e)
        {
            context.Response.StatusCode = 503;
            context.Response.WriteAsync("Unable to load start file. Please reach out to support");
        }
    }
);

app.MapPost(
    "/upload",
    async (HttpContext context) =>
    {
        UploadController uploadController = new UploadController(context);
        try
        {
            await uploadController.HandleUpload();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            context.Response.WriteAsync(e.Message);
            return;
        }
        await context.Response.WriteAsync($"Received file");
    }
);

app.MapGet(
    "/songs",
    async (HttpContext context) =>
    {
        string json = "";
        var list = Database.GetDatabase().Query<Song>("SELECT * from songs;");
        json += "[\n";
        bool firstLine = true;
        foreach (var row in list)
        {
            if (firstLine)
            {
                firstLine = false;
            }
            else
            {
                json += ",";
            }
            json += row.ToJson();
        }
        json += "\n]";

        Console.WriteLine(json);
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(json);
    }
);

app.MapGet(
    "/play/{id}",
    async (HttpContext context) =>
    {
        try
        {
            PlayController playController = new PlayController(context);
            string songName = await playController.Handle();
            SongManager songManager = new SongManager(songName);
            Task o = songManager.playAudio();
            //chnage 140x80 to something higher in order to see ascii art better
            Task c = FFmpegWrapper
                .GetFFmpegWrapper()
                .customCommandTest(
                    $" -i  ./bin/Mp4Files/{songName}.mp4 -s 140x80 -pix_fmt rgba -f image2pipe -vcodec png -"
                );

            Task.WaitAll(c, o);

            await context.Response.WriteAsync("Song found");
        }
        catch (Exception e)
        {
            await context.Response.WriteAsync(e.Message);
        }
    }
);

app.Run();
