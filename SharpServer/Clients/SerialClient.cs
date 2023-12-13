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
    private readonly List<byte> _buffer = new();

    public SerialClient(SerialPort port)
    {
        Port = port;
        Port.DataReceived += HandleDataReceived;
        Port.Open();
    }

    /// <summary>
    /// Callback to check if the buffer contains a COBS encoded message and if so, decode it and pass it to the message handlers
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

    private static void CopyToPipe(COBSWriterBuffer pipe, byte[] bytes)
    {
        var span = pipe.GetSpan(bytes.Length);
        bytes.CopyTo(span);
        pipe.Advance(bytes.Length);
    }

    public override void Send(IMessage msg)
    {
        if (!Port.IsOpen)
            throw new Exception($"Failed to send message to {Port.PortName} as its not open!");

        var msgId = BitConverter.GetBytes(MessageRegistry.GetIdByMessage(msg));
        var pipe = new Pipe();
        var cobsPipe = new COBSWriterBuffer(pipe.Writer);

        // Encode Message
        CopyToPipe(cobsPipe, msgId);

        // Special case for Ping and Pong messages as they don't have any data
        if (msg.Descriptor.GetType() != typeof(Ping) || msg.Descriptor.GetType() != typeof(Pong))
            CopyToPipe(cobsPipe, msg.ToByteArray());

        cobsPipe.CommitMessage();
        pipe.Writer.Complete();

        // Send Message
        // This feels horrible btw
        var outStream = new MemoryStream();
        pipe.Reader.CopyToAsync(outStream);
        SendRaw(outStream.ToArray());
        Console.WriteLine(BitConverter.ToString(outStream.ToArray()));
    }

    public override void SendRaw(byte[] msg)
    {
        Port.Write(msg, 0, msg.Length);
    }

    protected override void DisposeConnection()
    {
        Port.Close();
    }
}
