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
    public class DnsTlsUdpProxy : BackgroundService
    {
        private readonly ILogger _logger;
        private IOptions<AppConfig> _appConfig;

        public DnsTlsUdpProxy(ILogger<DnsTlsUdpProxy> logger, IOptions<AppConfig> appConfig)
        {
            _logger = logger;
            _appConfig = appConfig;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var server = new UdpClient(_appConfig.Value.LocalEndpoint);

            _logger.LogInformation($"UDP proxy started {_appConfig.Value.LocalEndpoint} -> {_appConfig.Value.DnsEndpoint}");
            while (true)
            {
                try
                {
                    var udpReceiveResult = await server.ReceiveAsync();
                    ProxyAsync(udpReceiveResult, server, _appConfig.Value.DnsEndpoint, stoppingToken);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "UDP proxy server failed");
                }
            }
        }

        private async void ProxyAsync(UdpReceiveResult udpReceiveResult, UdpClient client, IPEndPoint remoteEndpoint, CancellationToken stoppingToken)
        {
            try
            {
                var dnsClient = new DnsClient(_logger);

                var clientMsg = new DnsMessage(udpReceiveResult.Buffer);

                var serverMsg = await dnsClient.ResolveTlsAsync(remoteEndpoint, clientMsg, stoppingToken);

                await client.SendAsync(serverMsg.Data, serverMsg.Data.Length, udpReceiveResult.RemoteEndPoint);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "DNS question proxy failed");
            }

            _logger.LogDebug($"Closed {udpReceiveResult.RemoteEndPoint} => {remoteEndpoint}");
        }
    }
}