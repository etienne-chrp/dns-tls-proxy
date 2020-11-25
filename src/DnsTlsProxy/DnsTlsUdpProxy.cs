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
    public class DnsTlsUdpProxy : BackgroundService
    {
        private IOptions<AppConfig> _appConfig;

        public DnsTlsUdpProxy(IOptions<AppConfig> appConfig)
        {
            _appConfig = appConfig;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var server = new UdpClient(_appConfig.Value.LocalEndpoint);

            Console.WriteLine($"UDP proxy started {_appConfig.Value.LocalEndpoint} -> {_appConfig.Value.DnsEndpoint}");
            while (true)
            {
                try
                {
                    var udpReceiveResult = await server.ReceiveAsync();

                    Run(udpReceiveResult, server, _appConfig.Value.DnsEndpoint);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }

        private async void Run(UdpReceiveResult _udpReceiveResult, UdpClient client, IPEndPoint remoteEndpoint)
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
                        Console.WriteLine($"Established {_udpReceiveResult.RemoteEndPoint} => {remoteEndpoint}");

                        var data = _udpReceiveResult.Buffer;
                        byte[] length = BitConverter.GetBytes((ushort)data.Length);

                        if (BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(length);
                        }

                        await serverStream.WriteAsync(length, 0, length.Length);
                        await serverStream.WriteAsync(data, 0, data.Length);

                        length = new byte[2];
                        await serverStream.ReadAsync(length, 0, length.Length);

                        if (BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(length);
                        }

                        var buffer = new byte[BitConverter.ToUInt16(length)];
                        await serverStream.ReadAsync(buffer, 0, buffer.Length);

                        await client.SendAsync(buffer, buffer.Length, _udpReceiveResult.RemoteEndPoint);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            Console.WriteLine($"Closed {_udpReceiveResult.RemoteEndPoint} => {remoteEndpoint}");
        }
    }
}