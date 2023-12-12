using Google.Protobuf;
using SharpServer.Clients;

namespace SharpServer.Servers;

public abstract class Server : IMessageHandler, IDisposable
{
    protected readonly Dictionary<int, Client> _connectedClients = new();
    protected readonly Dictionary<int, Client> _limboClients = new();

    protected void InitializeClient(Client client)
    {
        client.OnTimeout += RemoveClient;
        client.OnConnected += AddClient;

        _limboClients.Add(client.Id, client);
    }

    private void AddClient(Client client)
    {
        _limboClients.Remove(client.Id);
        _connectedClients.Add(client.Id, client);
    }

    private void RemoveClient(Client client)
    {
        _connectedClients.Remove(client.Id);
    }

    /// <summary>
    /// Sends a message to all connected clients
    /// </summary>
    /// <param name="message">A Protobuffer message</param>
    public void SendAll(IMessage message)
    {
        foreach (var client in _connectedClients)
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
        foreach (var client in _connectedClients)
        {
            client.Value.SendRaw(bytes);
        }
    }

    public virtual void Dispose()
    {
        foreach (var client in _connectedClients)
            client.Value.Dispose();
        GC.SuppressFinalize(this);
    }
}
