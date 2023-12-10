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
    private readonly List<byte> _buffer = new();

    public SerialClient(SerialPort port)
    {
        Port = port;
        Port.DataReceived += HandleDataReceived;
        Port.Open();
    }

    private async void HandleDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        Console.WriteLine("Data received");
        while (Port.BytesToRead > 0)
        {
            var data = (byte)Port.ReadByte();
            if (data == 0x00)
            {
                Console.WriteLine("Found 00 byte");
                // Found a 00 byte, decode the message and handle it
                var reader = PipeReader.Create(new ReadOnlySequence<byte>(_buffer.ToArray()));
                await foreach (var msgByte in reader.ReadCOBSMessages())
                {
                    var msgId = MemoryMarshal.Read<int>(msgByte.Span);
                    var msg = MessageRegistry.GetMessageById(msgId);
                    msg.MergeFrom(msgByte[sizeof(int)..].ToArray());
                    HandleMessage(msg);
                }
                _buffer.Clear();
            }
            else
            {
                _buffer.Add(data);
            }
        }
    }

    protected override void Send(IMessage message)
    {
        if (!Port.IsOpen)
            throw new Exception("Port is not open");
        var msgId = MessageRegistry.GetIdByMessage(message);
        var bytes = message.ToByteArray();
        // TODO: encode with COBS

        var msg = new byte[bytes.Length + 1];
        msg[0] = (byte)msgId;
        bytes.CopyTo(msg, 1);

        Port.Write(msg, 0, msg.Length);
    }

    protected override void SendRaw(byte[] message)
    {
        Port.Write(message, 0, message.Length);
    }

    protected override void DisposeConnection()
    {
        Port.Close();
    }
}
