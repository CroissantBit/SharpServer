using Croissantbit;
using DotNetEnv;
using Google.Protobuf;
using Serilog;
using static SharpServer.Util;

namespace SharpServer.Clients;

public abstract class Client
{
    private static readonly int KeepAliveProbesLeftDefault = Env.GetInt("KEEPALIVE_PROBES", 5);

    public event Action<Client>? OnTimeout;
    public event Action<Client>? OnConnected;
    public int Id { get; } = GenerateClientId();

    private readonly Timer _keepAliveTimer;
    private int _keepAliveProbesLeft = KeepAliveProbesLeftDefault;

    public abstract void Send(IMessage message);
    public abstract void SendRaw(byte[] message);
    protected abstract void DisposeConnection();

    protected Client()
    {
        _keepAliveTimer = new Timer(
            RefreshKeepAliveState,
            null,
            TimeSpan.FromSeconds(Env.GetInt("KEEPALIVE_INTERVAL", 5000)),
            TimeSpan.FromSeconds(Env.GetInt("KEEPALIVE_INTERVAL", 5000))
        );
    }

    public void Dispose()
    {
        _keepAliveTimer.Dispose();
        DisposeConnection();
    }

    protected void HandleMessage(IMessage msg)
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

            case RegisterClientRequest:
                OnConnected?.Invoke(this);
                var response = new RegisterClientResponse
                {
                    ClientId = Id,
                    State = PlayerState.Idle
                };
                Send(response);
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
