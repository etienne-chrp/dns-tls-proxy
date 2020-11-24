using System;
using System.Net;

namespace DnsTlsProxy
{
    class Program
    {
        static void Main(string[] args)
        {
            try {
                var proxy = new TcpProxy();
                proxy.Start(
                    new IPEndPoint(IPAddress.Any, 5053),
                    new IPEndPoint(IPAddress.Parse("1.1.1.1"), 53)
                ).Wait();
            }
            catch (Exception e) {
                Console.WriteLine(e);
            }
        }
    }
}
