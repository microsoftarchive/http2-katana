using System.Security.Cryptography.X509Certificates;

namespace ServerProtocol
{
    /// <summary>
    /// Transport descriptor.
    /// </summary>
    public class TransportInformation
    {
        public string LocalIpAddress { get; set; }
        public string LocalPort { get; set; }
        public string RemoteIpAddress { get; set; }
        public string RemotePort { get; set; }
        public X509Certificate Certificate { get; set; }
    }
}
