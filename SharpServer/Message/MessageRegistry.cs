using Google.Protobuf;

namespace SharpServer.Message;

public static class MessageRegistry
{
    /*
     * Each message has an assigned ID that matches the order in the .proto file.
     * Will need to be updated manually on each new proto-spec version
     * This will also hopefully hint at the compiler to append messages at compile time
     * 2 List are used to allow faster lookups with the downside of requiring more memory
     */
    private static readonly Dictionary<int, IMessage> IdMessage =
        new()
        {
            { 1, new Croissantbit.Ping() },
            { 2, new Croissantbit.Pong() },
            { 3, new Croissantbit.RegisterClientRequest() },
            { 4, new Croissantbit.RegisterClientResponse() },
            { 5, new Croissantbit.VideoFrameUpdate() },
            { 6, new Croissantbit.AudioFrameUpdate() },
            { 7, new Croissantbit.VideoMetadataRequest() },
            { 8, new Croissantbit.VideoMetadataListRequest() },
            { 9, new Croissantbit.VideoMetadataResponse() },
            { 10, new Croissantbit.SignalSequenceFrameUpdate() },
            { 11, new Croissantbit.SignalUpdateRequest() },
            { 12, new Croissantbit.SignalUpdateResponse() },
            { 13, new Croissantbit.DeviceInfoRequest() },
            { 14, new Croissantbit.DeviceInfoListRequest() },
            { 15, new Croissantbit.DeviceInfoResponse() },
            { 16, new Croissantbit.PlayerPlayRequest() },
            { 17, new Croissantbit.PlayerStopRequest() },
            { 18, new Croissantbit.PlayerRequestResponse() },
            { 19, new Croissantbit.PlayerStateUpdate() }
        };

    private static readonly Dictionary<IMessage, int> MessageId =
        new()
        {
            { new Croissantbit.Ping(), 1 },
            { new Croissantbit.Pong(), 2 },
            { new Croissantbit.RegisterClientRequest(), 3 },
            { new Croissantbit.RegisterClientResponse(), 4 },
            { new Croissantbit.VideoFrameUpdate(), 5 },
            { new Croissantbit.AudioFrameUpdate(), 6 },
            { new Croissantbit.VideoMetadataRequest(), 7 },
            { new Croissantbit.VideoMetadataListRequest(), 8 },
            { new Croissantbit.VideoMetadataResponse(), 9 },
            { new Croissantbit.SignalSequenceFrameUpdate(), 10 },
            { new Croissantbit.SignalUpdateRequest(), 11 },
            { new Croissantbit.SignalUpdateResponse(), 12 },
            { new Croissantbit.DeviceInfoRequest(), 13 },
            { new Croissantbit.DeviceInfoListRequest(), 14 },
            { new Croissantbit.DeviceInfoResponse(), 15 },
            { new Croissantbit.PlayerPlayRequest(), 16 },
            { new Croissantbit.PlayerStopRequest(), 17 },
            { new Croissantbit.PlayerRequestResponse(), 18 },
            { new Croissantbit.PlayerStateUpdate(), 19 }
        };

    public static int GetIdByMessage(IMessage msg)
    {
        return MessageId[msg];
    }

    public static IMessage GetMessageById(int id)
    {
        return IdMessage[id];
    }
}
