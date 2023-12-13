using Croissantbit;
using Google.Protobuf;
using SharpServer.Clients;
using SharpServer.Message;

namespace SharpServer.Servers;

public abstract class Server : IMessageHandler, IDisposable
{
    public event Action<IMessage, Client>? OnMessageUpperManager;
    private readonly CancellationToken _cancellationToken;
    protected readonly Dictionary<int, Client> ConnectedClients = new();
    protected readonly Dictionary<int, Client> LimboClients = new();

    protected Server(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
    }

    protected void InitializeClient(Client client)
    {
        client.OnTimeout += RemoveClient;
        client.OnMessageUpperServer += HandleMessage;

        LimboClients.Add(client.Id, client);
    }

    public void AddClient(Client client)
    {
        LimboClients.Remove(client.Id);
        ConnectedClients.Add(client.Id, client);
    }

    private void RemoveClient(Client client)
    {
        ConnectedClients.Remove(client.Id);
    }

    /// <summary>
    /// Sends a message to all connected clients
    /// </summary>
    /// <param name="message">A Protobuffer message</param>
    public void SendAll(IMessage message)
    {
        foreach (var client in ConnectedClients)
        {
            client.Value.Send(message);
        }
    }

    /// <summary>
    /// Sends raw bytes to all connected clients
    /// </summary>
    /// <param name="bytes">Array of bytes</param>
    public void SendRawAll(byte[] bytes)
    {
        foreach (var client in ConnectedClients)
        {
            client.Value.SendRaw(bytes);
        }
    }

    public virtual void Dispose()
    {
        foreach (var client in ConnectedClients)
            client.Value.Dispose();
        GC.SuppressFinalize(this);
    }

    public void HandleMessage(IMessage msg, Client? client = null)
    {
        if (client == null)
            throw new ArgumentNullException(nameof(client));
        switch (msg)
        {
            case VideoMetadataRequest:

                break;

            case VideoMetadataListRequest:

                break;

            default:
                OnMessageUpperManager?.Invoke(msg, client);
                break;
        }
    }
}
