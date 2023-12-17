using Croissantbit;
using DotNetEnv;
using Google.Protobuf;
using Serilog;
using SharpServer.Clients;
using SharpServer.Controller;
using SharpServer.Database;
using SharpServer.FfmpegWrapper;
using SharpServer.Game;
using SharpServer.Remote;

namespace SharpServer.Servers;

public class HttpServer
{
    private GameManager _gameManager;
    private readonly WebApplication _app;

    public HttpServer(GameManager gameManager, CancellationToken ctx)
    {
        _gameManager = gameManager;
        Log.Information("Starting HTTP server...");
        var builder = WebApplication.CreateBuilder();
        builder
            .Services
            .AddCors(options =>
            {
                options.AddPolicy(
                    "AllowAllOrigins",
                    builder => builder.AllowAnyMethod().AllowAnyOrigin().AllowAnyHeader()
                );
            });
        _app = builder.Build();
        _app.UseCors("AllowAllOrigins");
        RegisterPaths();
        Console.WriteLine("isrunning");
        _app.RunAsync().WaitAsync(ctx);
    }

    private void RegisterPaths()
    {
        _app.MapGet(
            "/",
            async (context) =>
            {
                try
                {
                    var htmlContent = await File.ReadAllTextAsync("Pages/index.html");
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

        _app.MapPost(
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

        _app.MapGet(
            "/songs",
            async (context) =>
            {
                Console.WriteLine("uploading");
                var json = "";
                var list = DatabaseClient.GetDatabase().Query<Types.Video>("SELECT * from songs;");
                json += "[\n";
                var firstLine = true;
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

        _app.MapGet(
            "/play/{id}",
            async (HttpContext context, string id) =>
            {
                Console.WriteLine("");
                try
                {
                    PlayController playController = new PlayController(context);
                    string songName = await playController.Handle();
                    Console.WriteLine(songName);
                    var videoId = Convert.ToInt32(id);
                    //var video = VideoManager.GetVideo(videoId);
                    //_gameManager.PlayVideo(videoId);
                    Console.WriteLine(videoId);
                    //Console.WriteLine(video);
                    //!important
                    //add -framerate to define the specific framerate
                    var path = Env.GetString("CACHE_DIR") + "\\Mp4Files\\" + songName + ".mp4";
                    Console.WriteLine(path);
                    Task c = FFmpegWrapper
                        .GetFFmpegWrapper()
                        .CustomCommandTest(
                            $" -f mp4 -i  {path} -s 426x240 -vf fps=10 -pix_fmt rgba -f image2pipe -vcodec png -"
                        );

                    c.Wait();
                    await context.Response.WriteAsync("Found video with name " + songName);
                }
                catch (Exception e)
                {
                    await context.Response.WriteAsync(e.Message);
                }
            }
        );
    }
}
