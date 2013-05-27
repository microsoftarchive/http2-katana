using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace ServerProtocol
{
    public class TransportInformation
    {
        public string LocalIpAddress { get; set; }
        public string LocalPort { get; set; }
        public string RemoteIpAddress { get; set; }
        public string RemotePort { get; set; }
        public X509Certificate Certificate { get; set; }
    }
}
