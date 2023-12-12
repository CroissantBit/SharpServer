using System;
using System.Buffers;
using System.IO.Pipelines;
using System.IO.Ports;
using System.Runtime.InteropServices;
using FFT.COBS;
using Google.Protobuf;
using SharpServer.Message;

namespace SharpServer.Clients;

public class SerialClient : Client
{
    public SerialPort Port { get; }
    private readonly COBSWriterStream outputStream;
    private readonly List<byte> _buffer = new();


    public SerialClient(SerialPort port)
    {
        Port = port;
        outputStream = new COBSWriterStream(Port.BaseStream);
        Port.DataReceived += HandleDataReceived;
        Port.Open();
    }

    /// <summary>
    /// Callback to check if the buffer contains a COBS encoded message
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void HandleDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        // TODO consider using a stream
        // ex: Port.BaseStream.ReadCOBSMessages();
        while (Port.BytesToRead > 0)
        {
            var data = (byte)Port.ReadByte();
            _buffer.Add(data);
            if (data == 0x00)
            {
                // End of packet has been reached
                var reader = PipeReader.Create(new ReadOnlySequence<byte>(_buffer.ToArray()));
                await foreach (var msgByte in reader.ReadCOBSMessages())
                {
                    var msgId = MemoryMarshal.Read<short>(msgByte.Span[..sizeof(short)]);
                    var msg = MessageRegistry.GetMessageById(msgId);
                    msg.MergeFrom(msgByte.Span[sizeof(short)..]);
                    HandleMessage(msg);
                }
                _buffer.Clear();
            }
        }
    }

    public override void Send(IMessage message)
    {
        if (!Port.IsOpen)
            throw new Exception($"Failed to send message to {Port.PortName} as its not open!");
        var msgId = BitConverter.GetBytes(MessageRegistry.GetIdByMessage(message));
      
        outputStream.Write(msgId);
        message.WriteTo(outputStream);
        outputStream.CommitMessage();

        outputStream.Dispose();
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
