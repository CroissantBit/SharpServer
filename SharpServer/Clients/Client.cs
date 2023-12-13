using Croissantbit;
using DotNetEnv;
using Google.Protobuf;
using Serilog;
using SharpServer.Message;

namespace SharpServer.Clients;

public abstract class Client : IMessageHandler
{
    private static readonly int KeepAliveProbesLeftDefault = Env.GetInt("KEEPALIVE_PROBES", 5);

    public event Action<Client>? OnTimeout;
    public event Action<IMessage, Client>? OnMessageUpperServer;
    public int Id { get; } = Util.GenerateClientId();

    private readonly Timer _keepAliveTimer;
    private int _keepAliveProbesLeft = KeepAliveProbesLeftDefault;

    public abstract void Send(IMessage msg);
    public abstract void SendRaw(byte[] msg);
    protected abstract void DisposeConnection();

    protected Client()
    {
        _keepAliveTimer = new Timer(
            RefreshKeepAliveState,
            null,
            TimeSpan.FromMilliseconds(Env.GetInt("KEEPALIVE_INTERVAL", 8000)),
            TimeSpan.FromMilliseconds(Env.GetInt("KEEPALIVE_INTERVAL", 8000))
        );
    }

    public void Dispose()
    {
        _keepAliveTimer.Dispose();
        DisposeConnection();
    }

    public void HandleMessage(IMessage msg, Client? client = null)
    {
        Log.Debug($"Received message {msg} with type {msg.Descriptor.Name}");
        switch (msg)
        {
            case Ping:
                Send(new Pong());
                break;

            case Pong:
                _keepAliveProbesLeft = KeepAliveProbesLeftDefault;
                break;

            default:
                OnMessageUpperServer?.Invoke(msg, this);
                break;
        }
    }

    private void RefreshKeepAliveState(object? state)
    {
        Log.Debug("Refreshing keepalive state");
        if (_keepAliveProbesLeft == 0)
        {
            DisposeConnection();
            OnTimeout?.Invoke(this);
            return;
        }

        _keepAliveProbesLeft--;
        try
        {
            Send(new Ping());
        }
        catch (Exception e)
        {
            Log.Warning($"Failed to send Keepalive Ping with error: {e}");
        }
    }
}
