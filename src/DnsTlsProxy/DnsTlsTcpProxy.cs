using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace DnsTlsProxy
{
    public class DnsTlsTcpProxy
    {
        public async Task Start(IPEndPoint localEndpoint, IPEndPoint remoteEndpoint)
        {
            var server = new TcpListener(localEndpoint);
            server.Start();

            Console.WriteLine($"TCP proxy started {localEndpoint} -> {remoteEndpoint.Address}|{remoteEndpoint.Port}");
            while (true)
            {
                try
                {
                    var client = await server.AcceptTcpClientAsync();
                    client.NoDelay = true;

                    Run(client, remoteEndpoint);
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