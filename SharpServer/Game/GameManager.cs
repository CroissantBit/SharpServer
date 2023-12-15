using Croissantbit;
using Google.Protobuf;
using Serilog;
using SharpServer.Clients;
using SharpServer.Database;
using SharpServer.Message;
using SharpServer.Servers;
using SharpServer.Song;

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

        _playerState = PlayerState.Active;
        var playerState = new PlayerStateUpdate
        {
            State = _playerState,
            VideoMetadata = video.ToVideoMetadata()
        };
        SendToAll(playerState);
        var audioManager = new AudioManager(video.Name, HandleAudioStreamUpdate);
        // Let the clients prepare for the video
        Thread.Sleep(1000);
        var audioStreamTask = audioManager.Play();
        Log.Debug($"Playing video with id {video.Id}");

        audioStreamTask.Wait();
        Log.Debug($"Done playing video with id {video.Id}");
    }

    private void HandleAudioStreamUpdate(float value)
    {
        var signalStateUpdate = new SignalStateUpdate
        {
            Direction = value switch
            {
                < 0.2f => SignalDirection.Right,
                < 0.4f => SignalDirection.Left,
                < 0.6f => SignalDirection.Up,
                _ => SignalDirection.Down
            }
        };
        Console.WriteLine(signalStateUpdate.Direction);
        SendToAll(signalStateUpdate);
    }

    private void SendToAll(IMessage msg)
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
                    videoMetadataResponse.VideosMetadata.Add(video.ToVideoMetadata());

                client.Send(videoMetadataResponse);
                break;

            case PlayerPlayRequest playerPlayRequest:
                var playerPlayResponse = new PlayerRequestResponse();
                try
                {
                    if (_playerState != PlayerState.Idle)
                        throw new Exception(
                            "Player is currently active and cannot play another video"
                        );
                    playerPlayResponse.Success = true;
                    client.Send(playerPlayResponse);

                    PlayVideo(playerPlayRequest.VideoId);
                }
                catch (Exception e)
                {
                    Log.Warning($"Failed to handle PlayerPlayRequest: {e}");
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
