using System.Net;
using vtortola.WebSockets;
using vtortola.WebSockets.Deflate;
using vtortola.WebSockets.Rfc6455;

namespace SharpServer.Servers;

public class WebsocketServer
{
    public WebsocketServer(IPEndPoint endpoint)
    {
        // See: https://github.com/deniszykov/WebSocketListener/tree/master/Samples/EchoServer
        const int bufferSize = 1024 * 8; // 8KiB
        const int bufferPoolSize = 100 * bufferSize; // 800KiB pool
        var cancellation = new CancellationTokenSource();

        var options = new WebSocketListenerOptions
        {
            SubProtocols = new[] { "binary" },
            // We handle the ping manually
            PingMode = PingMode.Manual,
            ParallelNegotiations = 16,
            NegotiationQueueCapacity = 256,
            BufferManager = BufferManager.CreateBufferManager(bufferPoolSize, bufferSize)
        };
        options
            .Standards
            .RegisterRfc6455(factory =>
            {
                factory.MessageExtensions.RegisterDeflateCompression();
            });
        options
            .Transports
            .ConfigureTcp(transport =>
            {
                transport.ReceiveBufferSize = bufferSize;
                transport.SendBufferSize = bufferSize;
                transport.BacklogSize = 100;
            });

        var webSocketServer = new WebSocketListener(endpoint, options);
        webSocketServer.StartAsync().Wait();

        Console.WriteLine("Websocket server started");
        var acceptTask = AcceptWebSocketRequest(webSocketServer, cancellation.Token);
        Console.WriteLine("Waiting for websocket");

        acceptTask.Wait();
    }

    private static async Task AcceptWebSocketRequest(
        WebSocketListener webSocketListener,
        CancellationToken cancelToken
    )
    {
        await Task.Yield();
        while (!cancelToken.IsCancellationRequested)
            try
            {
                var webSocket = await webSocketListener
                    .AcceptWebSocketAsync(cancelToken)
                    .ConfigureAwait(false);
                if (webSocket == null)
                {
                    Console.WriteLine("Websocket not accepted");
                    if (cancelToken.IsCancellationRequested || !webSocketListener.IsStarted)
                        break; // Server stopped or cancellation requested
                    continue; // AcceptWebSocketAsync cancelled
                }

                Console.WriteLine("Websocket accepted");
#pragma warning disable CS4014
                // Because this call is not awaited, execution of the current method continues before the call is completed
                HandleConnection(webSocket, cancelToken);
#pragma warning restore CS4014
                // Because this call is not awaited, execution of the current method continues before the call is completed
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error accepting websocket: " + exception);
            }
    }

    private static async Task HandleConnection(WebSocket webSocket, CancellationToken cancellation)
    {
        Console.WriteLine("Handling connection: " + webSocket.RemoteEndpoint);

        try
        {
            while (webSocket.IsConnected && !cancellation.IsCancellationRequested)
                try
                {
                    Console.WriteLine("Waiting for new message");
                    // We expect the message to always to be in binary format due to Protobuf
                    var message = await webSocket
                        .ReadMessageAsync(cancellation)
                        .ConfigureAwait(false);
                    if (message == null)
                        break;

                    // Message doesn't contain any data, only the header
                    using var stream = new MemoryStream();
                    await message.CopyToAsync(stream, cancellation);
                    await message.CloseAsync();

                    // TODO
                    // Wait for ClientRegisterRequest
                    // In the meantime, respond to ping
                    // On ClientRegisterRequest, create a new Client with the websocket as the connection
                    Console.WriteLine("Client '" + webSocket.RemoteEndpoint + "' sent: " + stream);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception readWriteError)
                {
                    Console.WriteLine(
                        "An error occurred while reading/writing echo message." + readWriteError
                    );
                    break;
                }

            // Close socket before dispose
            await webSocket.CloseAsync(WebSocketCloseReason.NormalClose);
        }
        finally
        {
            // Always dispose socket after use
            webSocket.Dispose();
            Console.WriteLine("Client '" + webSocket.RemoteEndpoint + "' disconnected.");
        }
    }
}
