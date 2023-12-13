﻿using Google.Protobuf;

namespace SharpServer.Message;

public static class MessageRegistry
{
    /*
     * Each message has an assigned ID that matches the order in the .proto file.
     * Will need to be updated manually on each proto-spec version
     * This will also hopefully hint at the compiler to append messages at compile time
     * 2 List are used to allow faster lookups with the downside of requiring more memory
     */
    private static readonly Dictionary<short, IMessage> IdMessage =
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

    private static readonly Dictionary<string, short> MessageNameId =
        new()
        {
            { Croissantbit.Ping.Descriptor.Name, 1 },
            { Croissantbit.Pong.Descriptor.Name, 2 },
            { Croissantbit.RegisterClientRequest.Descriptor.Name, 3 },
            { Croissantbit.RegisterClientResponse.Descriptor.Name, 4 },
            { Croissantbit.VideoFrameUpdate.Descriptor.Name, 5 },
            { Croissantbit.AudioFrameUpdate.Descriptor.Name, 6 },
            { Croissantbit.VideoMetadataRequest.Descriptor.Name, 7 },
            { Croissantbit.VideoMetadataListRequest.Descriptor.Name, 8 },
            { Croissantbit.VideoMetadataResponse.Descriptor.Name, 9 },
            { Croissantbit.SignalSequenceFrameUpdate.Descriptor.Name, 10 },
            { Croissantbit.SignalUpdateRequest.Descriptor.Name, 11 },
            { Croissantbit.SignalUpdateResponse.Descriptor.Name, 12 },
            { Croissantbit.DeviceInfoRequest.Descriptor.Name, 13 },
            { Croissantbit.DeviceInfoListRequest.Descriptor.Name, 14 },
            { Croissantbit.DeviceInfoResponse.Descriptor.Name, 15 },
            { Croissantbit.PlayerPlayRequest.Descriptor.Name, 16 },
            { Croissantbit.PlayerStopRequest.Descriptor.Name, 17 },
            { Croissantbit.PlayerRequestResponse.Descriptor.Name, 18 },
            { Croissantbit.PlayerStateUpdate.Descriptor.Name, 19 }
        };

    public static short GetIdByMessage(IMessage msg)
    {
        return MessageNameId[msg.Descriptor.Name];
    }

    public static short GetIdByMessageName(string msgName)
    {
        return MessageNameId[msgName];
    }

    public static IMessage GetMessageById(short id)
    {
        return IdMessage[id];
    }
}
