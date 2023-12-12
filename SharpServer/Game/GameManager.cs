using SharpServer.Servers;

namespace SharpServer.Game
{
    public class GameManager
    {
        private readonly GameManagerServers _servers;

        public GameManager(GameManagerServers servers)
        {
            _servers = servers;
        }

        public Task StartServers(CancellationToken cancelToken)
        {
            if (_servers.HttpServer)
                new HttpServer(cancelToken);
            if (_servers.SerialServer)
                new SerialServer(cancelToken);
            if (_servers.WebsocketServer)
                throw new NotImplementedException();
            return Task.CompletedTask;
        }
    }

    public class GameManagerServers
    {
        public bool HttpServer { get; set; }
        public bool SerialServer { get; set; }
        public bool WebsocketServer { get; set; }
    }
}
