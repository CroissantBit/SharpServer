using DotNetEnv;
using FfmpegWrapper;
using Serilog;
using SharpServer;
using SharpServer.Game;

// Set up the environment
Env.Load();
Log.Logger = SharpLogger.Initialize();
FFmpegConfig.SetFFmpegConfig("./bin/ffmpeg.exe", false, true);

Log.Information("Starting servers...");

Console.WriteLine("Press CTRL+C to exit manually");
var cancelToken = new CancellationTokenSource();
var servers = new GameManagerServers
{
    HttpServer = Env.GetBool("START_HTTP_SERVER"),
    SerialServer = Env.GetBool("START_SERIAL_SERVER"),
    WebsocketServer = Env.GetBool("START_WEBSOCKET_SERVER")
};
if (servers is { HttpServer: false, SerialServer: false, WebsocketServer: false })
{
    Log.Warning("No servers are enabled. Please enable at least one server in your .env file");
    cancelToken.Cancel();
}

var gameManager = new GameManager(servers);
await gameManager.StartServers(cancelToken.Token);

Console.CancelKeyPress += (_, args) =>
{
    Log.Information("Stopping...");
    cancelToken.Cancel();
    args.Cancel = true;
};

cancelToken.Token.WaitHandle.WaitOne();
