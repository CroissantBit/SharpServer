using Google.Protobuf;
using SharpServer.Clients;

namespace SharpServer.Servers;

public abstract class Server : IMessageHandler, IDisposable
{
    protected readonly Dictionary<int, Client> ConnectedClients = new();
    protected readonly Dictionary<int, Client> LimboClients = new();

    public Server(CancellationToken cancellationToken) { }

    protected void InitializeClient(Client client)
    {
        client.OnTimeout += RemoveClient;
        client.OnConnected += AddClient;

        LimboClients.Add(client.Id, client);
    }

    private void AddClient(Client client)
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
}
