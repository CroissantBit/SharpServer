using Croissantbit;
using Google.Protobuf;
using Serilog;
using SharpServer.Clients;
using SharpServer.Database;
using SharpServer.FfmpegWrapper;
using SharpServer.Message;
using SharpServer.Servers;

namespace SharpServer.Game;

public class GameManager : IMessageHandler
{
    private readonly GameManagerServers _servers;
    private Server? _serialServer;
    private HttpServer? _httpServer;

    private PlayerState _playerState = PlayerState.Idle;

    public GameManager(GameManagerServers servers)
    {
        _servers = servers;
    }

    public Task StartServers(CancellationToken cancelToken)
    {
        if (_servers.HttpServer)
        {
            _httpServer = new HttpServer(cancelToken);
            _httpServer.OnMessageUpperManager += HandleMessage;
        }

        if (_servers.SerialServer)
        {
            _serialServer = new SerialServer(cancelToken);
            _serialServer.OnMessageUpperManager += HandleMessage;
        }

        if (_servers.WebsocketServer)
            throw new NotImplementedException();
        return Task.CompletedTask;
    }

    public void PlayVideo(int videoId)
    {
        var video = VideoManager.GetVideo(videoId);
        // TODO
        // FFmpegWrapper.GetFFmpegWrapper().GetVideoStream(video)
        var stream = null as Stream;

        var task = Task.Run(async () =>
        {
            await Task.Delay(5000);
            var playerState = new PlayerStateUpdate
            {
                State = _playerState,
                VideoMetadata = video.toVideoMetadata()
            };
            SendToAll(playerState);
        });

        var pixels = VideoStream.ReadFrame(stream, 240, 280);

        Console.WriteLine(pixels.Length);
        foreach (var pixel in pixels)
            Console.WriteLine(pixel);
        Console.WriteLine("Done");
    }

    public void SendToAll(IMessage msg)
    {
        _serialServer?.SendAll(msg);
    }

    public void HandleMessage(IMessage msg, Client? client = null)
    {
        if (client == null)
            throw new ArgumentNullException(nameof(client));
        switch (msg)
        {
            case RegisterClientRequest:
                _serialServer?.AddClient(client);
                var response = new RegisterClientResponse
                {
                    ClientId = client.Id,
                    State = _playerState
                };
                client.Send(response);
                break;

            case VideoMetadataRequest videoMetadataRequest:
                var requestedVideoIds = string.Join(",", videoMetadataRequest.VideoIds);
                var videoList = DatabaseClient
                    .GetDatabase()
                    .Query<Types.Video>($"SELECT * from songs where id in ({requestedVideoIds});");

                var videoMetadataResponse = new VideoMetadataResponse();
                foreach (var video in videoList)
                    videoMetadataResponse.VideosMetadata.Add(video.toVideoMetadata());

                client.Send(videoMetadataResponse);
                break;

            case PlayerPlayRequest playerPlayRequest:
                var playerPlayResponse = new PlayerRequestResponse();
                try
                {
                    if (_playerState != PlayerState.Idle)
                        throw new Exception("Player is not idle");

                    var video = VideoManager.GetVideo(playerPlayRequest.VideoId);
                    playerPlayResponse.Success = true;
                    client.Send(playerPlayResponse);

                    // Inform all clients that the video playback is about to start
                    _playerState = PlayerState.Active;
                    var playerState = new PlayerStateUpdate
                    {
                        State = _playerState,
                        VideoMetadata = video.toVideoMetadata()
                    };
                    SendToAll(playerState);
                }
                catch (Exception e)
                {
                    Log.Debug($"Failed to handle PlayerPlayRequest: {e}");
                    playerPlayResponse.Success = false;
                    client.Send(playerPlayResponse);
                }

                break;

            case PlayerStopRequest:
                break;
        }
    }
}

public class GameManagerServers
{
    public bool HttpServer { get; init; }
    public bool SerialServer { get; init; }
    public bool WebsocketServer { get; init; }
}
