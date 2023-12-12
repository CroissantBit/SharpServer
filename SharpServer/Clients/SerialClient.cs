using System;
using System.Buffers;
using System.IO.Pipelines;
using System.IO.Ports;
using System.Runtime.InteropServices;
using Croissantbit;
using FFT.COBS;
using Google.Protobuf;
using Serilog;
using SharpServer.Message;

namespace SharpServer.Clients;

public class SerialClient : Client
{
    public SerialPort Port { get; }
    private readonly COBSWriterBuffer _outputStream;
    private readonly List<byte> _buffer = new();

    public SerialClient(SerialPort port)
    {
        Port = port;
        Port.DataReceived += HandleDataReceived;
        Port.Open();

        _outputStream = new COBSWriterBuffer(Port.BaseStream);
    }

    /// <summary>
    /// Callback to check if the buffer contains a COBS encoded message
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void HandleDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        // TODO consider using a stream
        // ex: Port.BaseStream.ReadCOBSMessages(); with a cancellation token to stop reading
        while (Port.BytesToRead > 0)
        {
            var data = (byte)Port.ReadByte();
            _buffer.Add(data);

            if (data != 0x00)
                continue;
            try
            {
                // End of packet has been reached
                var reader = PipeReader.Create(new ReadOnlySequence<byte>(_buffer.ToArray()));
                await foreach (var msgByte in reader.ReadCOBSMessages())
                {
                    var msgId = MemoryMarshal.Read<short>(msgByte.Span[..sizeof(short)]);
                    var msg = MessageRegistry.GetMessageById(msgId);

                    // Special case for Ping and Pong messages as they don't have any data
                    if (
                        msg.Descriptor.GetType() != typeof(Ping)
                        || msg.Descriptor.GetType() != typeof(Pong)
                    )
                        msg.MergeFrom(msgByte.Span[sizeof(short)..]);
                    HandleMessage(msg);
                }

                _buffer.Clear();
            }
            catch (Exception exception)
            {
                Log.Warning($"Failed to decode message from serial port: {exception}");
            }
        }
    }

    public override void Send(IMessage message)
    {
        if (!Port.IsOpen)
            throw new Exception($"Failed to send message to {Port.PortName} as its not open!");

        // SendRaw(new byte[] { 0x02, 0x01, 0x01, 0x00 });
        var msgId = BitConverter.GetBytes(MessageRegistry.GetIdByMessage(message));

        _outputStream.Write(msgId);
        if (message.CalculateSize() > 0)
            message.WriteTo(_outputStream);
        _outputStream.CommitMessage();
        Port.BaseStream.Flush();
    }

    public override void SendRaw(byte[] message)
    {
        Port.Write(message, 0, message.Length);
    }

    protected override void DisposeConnection()
    {
        Port.Close();
    }
}