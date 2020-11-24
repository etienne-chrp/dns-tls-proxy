using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace DnsTlsProxy
{
    public class TcpProxy
    {
        public async Task Start(IPEndPoint localServer, IPEndPoint remoteServer)
        {
            var server = new System.Net.Sockets.TcpListener(localServer);
            server.Start();

            Console.WriteLine($"TCP proxy started {localServer.Port} -> {remoteServer.Address}|{remoteServer.Port}");
            while (true)
            {

                try
                {
                    var remoteClient = await server.AcceptTcpClientAsync();
                    remoteClient.NoDelay = true;

                    new TcpClientCustom(remoteClient, remoteServer);
                }
                catch (Exception ex)
                {
                    // TODO add proper logger
                    Console.WriteLine(ex);
                }
            }
        }
    }

    class TcpClientCustom
    {
        private TcpClient _client;
        private TcpClient _server = new TcpClient();
        private IPEndPoint _clientEndpoint;
        private IPEndPoint _serverEndpoint;


        public TcpClientCustom(TcpClient localClient, IPEndPoint remoteEndpoint)
        {
            _client = localClient;
            _serverEndpoint = remoteEndpoint;

            _clientEndpoint = (IPEndPoint)_client.Client.RemoteEndPoint;

            _server.NoDelay = true;

            Run();
        }

        private void Run()
        {
            Task.Run(async () =>
            {
                try
                {
                    using (_client)
                    using (_server)
                    {
                        await _server.ConnectAsync(_serverEndpoint.Address, _serverEndpoint.Port);
                        Console.WriteLine($"Established {_clientEndpoint} => {_serverEndpoint}");

                        var serverStream = _server.GetStream();
                        var clientStream = _client.GetStream();

                        await Task.WhenAny(clientStream.CopyToAsync(serverStream), serverStream.CopyToAsync(clientStream));
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                Console.WriteLine($"Closed {_clientEndpoint} => {_serverEndpoint}");
            });
        }
    }
}