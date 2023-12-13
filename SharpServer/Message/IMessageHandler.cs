using Google.Protobuf;
using SharpServer.Clients;

namespace SharpServer.Message;

public interface IMessageHandler
{
    protected void HandleMessage(IMessage msg, Client? client = null);
}
