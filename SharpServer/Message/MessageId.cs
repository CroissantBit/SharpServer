using Google.Protobuf;

namespace SharpServer.Message;

public class MessageId
{
    private static readonly Dictionary<IMessage, int> MessageIdByType = new();
    private static readonly Dictionary<int, IMessage> MessageById = new();

    public static MessageId Instance { get; } = new MessageId();

    /*
     * Each message has an assigned ID that matches the order in the .proto file.
     * Will need to be updated manually on each new proto-spec iteration
     */
    private MessageId()
    {
        AppendMessage(new Croissantbit.Ping(), 1);
        AppendMessage(new Croissantbit.Pong(), 2);
        AppendMessage(new Croissantbit.RegisterClientRequest(), 3);
        AppendMessage(new Croissantbit.RegisterClientResponse(), 4);
        AppendMessage(new Croissantbit.VideoFrameUpdate(), 5);
        AppendMessage(new Croissantbit.AudioFrameUpdate(), 6);
        AppendMessage(new Croissantbit.VideoMetadataRequest(), 7);
        AppendMessage(new Croissantbit.VideoMetadataListRequest(), 8);
        AppendMessage(new Croissantbit.VideoMetadataResponse(), 9);
        AppendMessage(new Croissantbit.SignalSequenceFrameUpdate(), 10);
        AppendMessage(new Croissantbit.SignalUpdateRequest(), 11);
        AppendMessage(new Croissantbit.SignalUpdateResponse(), 12);
        AppendMessage(new Croissantbit.DeviceInfoRequest(), 13);
        AppendMessage(new Croissantbit.DeviceInfoListRequest(), 14);
        AppendMessage(new Croissantbit.DeviceInfoResponse(), 15);
    }

    private static void AppendMessage(IMessage message, int id)
    {
        MessageIdByType.Add(message, id);
        MessageById.Add(id, message);
    }

    public static int GetMessageId(IMessage message)
    {
        return MessageIdByType[message];
    }

    public static IMessage GetMessage(int messageId)
    {
        return MessageById[messageId];
    }
}
