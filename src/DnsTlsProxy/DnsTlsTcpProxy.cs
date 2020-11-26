using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace DnsTlsProxy
{
    public class DnsTlsTcpProxy : BackgroundService
    {
        private IOptions<AppConfig> _appConfig;

        public DnsTlsTcpProxy(IOptions<AppConfig> appConfig)
        {
            _appConfig = appConfig;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var server = new TcpListener(_appConfig.Value.LocalEndpoint);
            server.Start();

            Console.WriteLine($"TCP proxy started {_appConfig.Value.LocalEndpoint} -> {_appConfig.Value.DnsEndpoint}");
            while (true)
            {
                try
                {
                    var client = await server.AcceptTcpClientAsync();
                    client.NoDelay = true;

                    Run(client, _appConfig.Value.DnsEndpoint);

                }
                catch (Exception ex)
                {
                    // TODO add proper logger
                    Console.WriteLine(ex);
                }
            }
        }

        private async void Run(TcpClient client, IPEndPoint remoteEndpoint)
        {
            try
            {
                using (var server = new TcpClient())
                {
                    server.NoDelay = true;
                    await server.ConnectAsync(remoteEndpoint.Address, remoteEndpoint.Port);

                    using (SslStream serverStream = new SslStream(
                        server.GetStream(),
                        false,
                        new RemoteCertificateValidationCallback(SslStreamHelper.ValidateServerCertificate)))
                    {
                        await serverStream.AuthenticateAsClientAsync(remoteEndpoint.Address.ToString());
                        Console.WriteLine($"Established {client.Client.RemoteEndPoint} => {remoteEndpoint}");

                        var clientStream = client.GetStream();

                        await Task.WhenAny(clientStream.CopyToAsync(serverStream), serverStream.CopyToAsync(clientStream));
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            Console.WriteLine($"Closed {client.Client.RemoteEndPoint} => {remoteEndpoint}");
        }
    }
}