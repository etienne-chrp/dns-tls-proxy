using System;
using System.Net;

namespace DnsTlsProxy
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var tcpProxyTask = new DnsTlsTcpProxy().Start(
                    new IPEndPoint(IPAddress.Any, 5053),
                    new IPEndPoint(IPAddress.Parse("1.1.1.1"), 853)
                );

                tcpProxyTask.Wait();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
