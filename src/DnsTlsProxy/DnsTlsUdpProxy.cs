using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace DnsTlsProxy
{
    public class DnsTlsUdpProxy
    {
        public async Task Start(IPEndPoint localEndpoint, IPEndPoint remoteEndpoint)
        {
            var server = new UdpClient(localEndpoint);

            Console.WriteLine($"UDP proxy started {localEndpoint} -> {remoteEndpoint}");
            while (true)
            {
                try
                {
                    var udpReceiveResult = await server.ReceiveAsync();

                    Run(udpReceiveResult, server, remoteEndpoint);
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