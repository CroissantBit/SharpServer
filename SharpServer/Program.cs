using System.Reflection;
using FfmpegWrapper;
using Serilog;
using SharpServer;
using SharpServer.Database;
using SharpServer.FfmpegWrapper;
using SharpServer.Game;
using SharpServer.Servers;
using SharpServer.Song;
using SharpServer.Types;
using SharpServer.Upload;

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
    async (context) =>
    {
        try
        {
            string htmlContent = await File.ReadAllTextAsync("Pages/index.html");
            context.Response.ContentType = "text/html";
            context.Response.StatusCode = 200;
            await context.Response.WriteAsync(htmlContent);
        }
        catch (Exception)
        {
            context.Response.StatusCode = 503;
            await context
                .Response
                .WriteAsync("Unable to load start file. Please reach out to support");
        }
    }
);

app.MapPost(
    "/upload",
    async (context) =>
    {
        var uploadController = new UploadController(context);
        try
        {
            await uploadController.HandleUpload();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            await context.Response.WriteAsync(e.Message);
            return;
        }

        await context.Response.WriteAsync($"Received file");
    }
);

app.MapGet(
    "/songs",
    async (context) =>
    {
        string json = "";
        var list = DatabaseClient.GetDatabase().Query<Song>("SELECT * from songs;");
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
    async (context) =>
    {
        try
        {
            PlayController playController = new PlayController(context);
            string songName;
            try
            {
                songName = await playController.Handle();
            }
            catch (Exception e)
            {
                await context.Response.WriteAsync(e.Message);
                return;
            }

            SongManager songManager = new SongManager(songName);
            Task o = songManager.PlayAudio();
            //chnage 140x80 to something higher in order to see ascii art better
            Task c = FFmpegWrapper
                .GetFFmpegWrapper()
                .CustomCommandTest(
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

Log.Information("Starting server...");
var server = new SerialServer();
app.Run();
