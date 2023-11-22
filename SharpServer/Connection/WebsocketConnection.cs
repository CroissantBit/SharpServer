using Google.Protobuf;
using vtortola.WebSockets;

namespace SharpServer.Connection;

public class WebsocketConnection : Connection
{
    private WebSocket _socket;

    public override void Send(IMessage message) { }

    public override void SendRaw(byte[] message) { }
}
