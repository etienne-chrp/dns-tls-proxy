using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace DnsTlsProxy
{
    public class DnsTlsTcpProxy : BackgroundService
    {
        private readonly ILogger _logger;
        private IOptions<AppConfig> _appConfig;

        public DnsTlsTcpProxy(ILogger<DnsTlsTcpProxy> logger, IOptions<AppConfig> appConfig)
        {
            _logger = logger;
            _appConfig = appConfig;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var server = new TcpListener(_appConfig.Value.LocalEndpoint);
            server.Start();

            _logger.LogInformation($"TCP proxy started {_appConfig.Value.LocalEndpoint} -> {_appConfig.Value.DnsEndpoint}");
            while (true)
            {
                try
                {
                    var client = await server.AcceptTcpClientAsync();
                    client.NoDelay = true;

                    Run(client, _appConfig.Value.DnsEndpoint);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "TCP proxy server failed");
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
                        new RemoteCertificateValidationCallback(new SslStreamHelper(_logger).ValidateServerCertificate)))
                    {
                        await serverStream.AuthenticateAsClientAsync(remoteEndpoint.Address.ToString());
                        _logger.LogDebug($"Established {client.Client.RemoteEndPoint} => {remoteEndpoint}");

                        var clientStream = client.GetStream();

                        await Task.WhenAny(clientStream.CopyToAsync(serverStream), serverStream.CopyToAsync(clientStream));
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "DNS question proxy failed");
            }

            _logger.LogDebug($"Closed {client.Client.RemoteEndPoint} => {remoteEndpoint}");
        }
    }
}