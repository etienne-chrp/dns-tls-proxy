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

                        var length = new byte[2];
                        await clientStream.ReadAsync(length, 0, length.Length, stoppingToken);

                        // Different computer architectures store data using different byte orders.
                        // "Little-endian" means the most significant byte is on the right end of a word.
                        // https://docs.microsoft.com/en-us/dotnet/api/system.bitconverter.islittleendian
                        if (BitConverter.IsLittleEndian)
                            Array.Reverse(length);

                        var buffer = new byte[BitConverter.ToUInt16(length)];
                        await clientStream.ReadAsync(buffer, 0, buffer.Length, stoppingToken);

                        if (BitConverter.IsLittleEndian)
                            Array.Reverse(length);

                        await serverStream.WriteAsync(length, 0, length.Length, stoppingToken);
                        await serverStream.WriteAsync(buffer, 0, buffer.Length, stoppingToken);

                        length = new byte[2];
                        await serverStream.ReadAsync(length, 0, length.Length, stoppingToken);

                        if (BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(length);
                        }

                        buffer = new byte[BitConverter.ToUInt16(length)];
                        await serverStream.ReadAsync(buffer, 0, buffer.Length, stoppingToken);

                        if (BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(length);
                        }

                        await clientStream.WriteAsync(length, 0, length.Length, stoppingToken);
                        await clientStream.WriteAsync(buffer, 0, buffer.Length, stoppingToken);
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