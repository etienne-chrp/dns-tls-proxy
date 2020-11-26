using System;
using System.Net;
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

                    Run(client, _appConfig.Value.DnsEndpoint, stoppingToken);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "TCP proxy server failed");
                }
            }
        }

        private async void Run(TcpClient client, IPEndPoint remoteEndpoint, CancellationToken stoppingToken)
        {
            try
            {
                var dnsClient = new DnsClient(_logger);

                var clientStream = client.GetStream();
                var clientMsg = await dnsClient.ReadTcpAsync(clientStream, stoppingToken);

                var serverMsg = await dnsClient.ResolveTlsAsync(remoteEndpoint, clientMsg, stoppingToken);

                await dnsClient.SendTcpAsync(clientStream, serverMsg, stoppingToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "DNS question proxy failed");
            }

            _logger.LogDebug($"Closed {client.Client.RemoteEndPoint} => {remoteEndpoint}");
        }
    }
}