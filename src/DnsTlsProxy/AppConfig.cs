using System.Net;

namespace DnsTlsProxy
{
    public class AppConfig
    {
        public string LocalIp { get; set; }
        public int LocalPort { get; set; }
        public IPEndPoint LocalEndpoint => new IPEndPoint(
            IPAddress.Parse(LocalIp),
            LocalPort
        );

        public string DnsIp { get; set; }
        public int DnsPort { get; set; }
        public IPEndPoint DnsEndpoint => new IPEndPoint(
            IPAddress.Parse(DnsIp),
            DnsPort
        );
        public string DnsCN { get; set; }
    }
}