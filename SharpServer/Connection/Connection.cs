using Google.Protobuf;

namespace SharpServer.Connection;

public abstract class Connection
{
    public abstract void Send(IMessage message);
    public abstract void SendRaw(byte[] message);
}
