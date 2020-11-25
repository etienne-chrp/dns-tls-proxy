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
                    Run(udpReceiveResult, server, _appConfig.Value.DnsEndpoint, stoppingToken);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "UDP proxy server failed");
                }
            }
        }

        private async void Run(UdpReceiveResult _udpReceiveResult, UdpClient client, IPEndPoint remoteEndpoint, CancellationToken stoppingToken)
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
                        _logger.LogDebug($"Established {_udpReceiveResult.RemoteEndPoint} => {remoteEndpoint}");

                        var data = _udpReceiveResult.Buffer;
                        byte[] length = BitConverter.GetBytes((ushort)data.Length);

                        if (BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(length);
                        }

                        await serverStream.WriteAsync(length, 0, length.Length, stoppingToken);
                        await serverStream.WriteAsync(data, 0, data.Length, stoppingToken);

                        length = new byte[2];
                        await serverStream.ReadAsync(length, 0, length.Length, stoppingToken);

                        if (BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(length);
                        }

                        var buffer = new byte[BitConverter.ToUInt16(length)];
                        await serverStream.ReadAsync(buffer, 0, buffer.Length, stoppingToken);

                        await client.SendAsync(buffer, buffer.Length, _udpReceiveResult.RemoteEndPoint);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "DNS question proxy failed");
            }

            _logger.LogDebug($"Closed {_udpReceiveResult.RemoteEndPoint} => {remoteEndpoint}");
        }
    }
}