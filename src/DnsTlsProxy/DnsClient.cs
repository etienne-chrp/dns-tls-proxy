using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;
using System.Net.Security;

namespace DnsTlsProxy
{
    public partial class DnsClient
    {
        private ILogger _logger;

        public DnsClient(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<DnsMessage> ReadTcpAsync(Stream stream, CancellationToken stoppingToken)
        {
            var length = new byte[2];
            await stream.ReadAsync(length, 0, length.Length, stoppingToken);
            length = DnsMessage.ConvertLittleEndian(length);

            var buffer = new byte[BitConverter.ToUInt16(length)];
            await stream.ReadAsync(buffer, 0, buffer.Length, stoppingToken);

            return new DnsMessage(buffer);
        }

        public async Task SendTcpAsync(Stream stream, DnsMessage message, CancellationToken stoppingToken)
        {
            await stream.WriteAsync(message.Length, 0, message.Length.Length, stoppingToken);
            await stream.WriteAsync(message.Data, 0, message.Data.Length, stoppingToken);
        }

        public async Task<DnsMessage> ResolveTlsAsync(IPEndPoint serverEndpoint, string serverCN, DnsMessage message, CancellationToken stoppingToken)
        {
            using (var server = new TcpClient())
            {
                server.NoDelay = true;
                await server.ConnectAsync(serverEndpoint.Address, serverEndpoint.Port);

                using (SslStream serverStream = new SslStream(
                    server.GetStream(),
                    false,
                    new RemoteCertificateValidationCallback(new SslStreamHelper(_logger).ValidateServerCertificate)))
                {
                    await serverStream.AuthenticateAsClientAsync(serverCN);
                    _logger.LogDebug($"Established {serverEndpoint}");

                    await SendTcpAsync(serverStream, message, stoppingToken);

                    return await ReadTcpAsync(serverStream, stoppingToken);
                }
            }
        }
    }
}