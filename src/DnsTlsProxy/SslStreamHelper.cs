using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;

namespace DnsTlsProxy
{
    public class SslStreamHelper
    {
        private readonly ILogger _logger;

        public SslStreamHelper(ILogger logger)
        {
            _logger = logger;
        }

        public bool ValidateServerCertificate(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            _logger.LogDebug("Verifying certificate");

            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            _logger.LogError($"Certificate error: {sslPolicyErrors}");

            // Do not allow this client to communicate with unauthenticated servers.
            return false;
        }
    }
}