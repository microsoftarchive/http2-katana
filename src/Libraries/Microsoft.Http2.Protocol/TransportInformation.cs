using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Http2.Protocol
{
    /// <summary>
    /// Transport descriptor.
    /// </summary>
    public class TransportInformation
    {
        public string LocalIpAddress { get; set; }
        public int LocalPort { get; set; }
        public string RemoteIpAddress { get; set; }
        public int RemotePort { get; set; }
        public X509Certificate Certificate { get; set; }
    }
}
