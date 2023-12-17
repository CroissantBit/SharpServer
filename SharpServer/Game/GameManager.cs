using Croissantbit;
using DotNetEnv;
using Google.Protobuf;
using Serilog;
using SharpServer.Clients;
using SharpServer.Database;
using SharpServer.FfmpegWrapper;
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
    private SignalDirection _currentSignalDirection;

    public GameManager(GameManagerServers servers)
    {
        _servers = servers;
    }

    public Task StartServers(CancellationToken cancelToken)
    {
        if (_servers.HttpServer)
        {
            _httpServer = new HttpServer(this, cancelToken);
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

    public Task PlayVideo(int videoId)
    {
        var video = VideoManager.GetVideo(videoId);
        var path = Env.GetString("CACHE_DIR") + @"\Mp4Files\" + video.Name + ".mp4";

        var audioManager = new AudioManager(video.Name, HandleAudioStreamUpdate);
        audioManager.GenerateAudioMap();

        Log.Debug($"Playing video with id {video.Id}");
        _playerState = PlayerState.Active;
        var playerState = new PlayerStateUpdate { State = _playerState };
        SendToAll(playerState);

        // Let the clients prepare for the video
        Thread.Sleep(1000);

        var wrapper = FFmpegWrapper.GetFFmpegWrapper();
        Parallel.Invoke(
            () => audioManager.Play(),
            () =>
                wrapper.CustomCommandTest(
                    $" -f mp4 -i  {path} -s 426x240 -vf fps=10 -pix_fmt rgba -f image2pipe -vcodec png -"
                )
        );

        Thread.Sleep(1000);
        Log.Debug($"Done playing video with id {video.Id}");
        _playerState = PlayerState.Idle;
        playerState.State = _playerState;
        SendToAll(playerState);

        return Task.CompletedTask;
    }

    private void HandleAudioStreamUpdate(float value)
    {
        var randomness = new Random().NextSingle();
        if (randomness > 0.5)
            value += randomness % 0.2F;
        else
            value -= randomness % 0.2F;

        var signalStateUpdate = new SignalStateUpdate
        {
            Direction = value switch
            {
                < 0.25f => SignalDirection.Right,
                < 0.5f => SignalDirection.Left,
                < 0.75f => SignalDirection.Up,
                _ => SignalDirection.Down
            }
        };
        Log.Debug(signalStateUpdate.Direction.ToString());
        _currentSignalDirection = signalStateUpdate.Direction;
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
            case RegisterClientRequest registerClientRequest:
                client.ScreenHeight = registerClientRequest.Height;
                client.ScreenWidth = registerClientRequest.Width;

                try
                {
                    _serialServer?.AddClient(client);
                }
                catch (Exception e)
                {
                    Log.Warning($"Failed to add client to serial server: {e}");
                }

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

            case SignalUpdateRequest signalUpdateRequest:
                if (_playerState != PlayerState.Active)
                    break;
                SignalUpdateResponse signalUpdateResponse = new();

                // Enum might not be defined by the client, if so we treat is as a left signal
                if (!Enum.IsDefined(signalUpdateRequest.Direction))
                {
                    signalUpdateResponse.Success = true;
                }
                else
                {
                    signalUpdateResponse.Success =
                        signalUpdateRequest.Direction == _currentSignalDirection;
                }

                client.Send(signalUpdateResponse);
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
                if (_playerState == PlayerState.Idle)
                    break;
                _playerState = PlayerState.Idle;
                var playerStateUpdate = new PlayerStateUpdate { State = _playerState };
                SendToAll(playerStateUpdate);
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
