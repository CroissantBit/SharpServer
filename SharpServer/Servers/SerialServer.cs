using System.IO.Ports;
using DotNetEnv;
using Serilog;
using SharpServer.Clients;

namespace SharpServer.Servers;

public class SerialServer : Server
{
    private readonly Timer _timer;

    public SerialServer()
    {
        _timer = new Timer(ScanForClients, null, 0, Env.GetInt("SERIAL_SCAN_INTERVAL", 8000));
    }

    private async void ScanForClients(object? state)
    {
        Log.Information("Scanning for serial clients...");
        var serialClients = _connectedClients
            .Values
            .OfType<SerialClient>()
            .Concat(_limboClients.Values.OfType<SerialClient>());
        var serialPorts = SerialPort
            .GetPortNames()
            .Where(port => serialClients.All(client => client.Port.PortName != port))
            .ToArray();
        Log.Information($"Trying to connect to {serialPorts.Length} serial ports");
        foreach (var port in serialPorts)
        {
            try
            {
                var serialPort = new SerialPort(port, Env.GetInt("SERIAL_BAUD_RATE", 115200));
                var client = new SerialClient(serialPort);
                InitializeClient(client);
            }
            catch (Exception e)
            {
                Log.Warning($"Failed to connect to {port} with error {e.Message}");
            }
        }
    }

    public override void Dispose()
    {
        _timer.Dispose();
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}
