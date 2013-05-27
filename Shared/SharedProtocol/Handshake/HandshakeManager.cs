using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Org.Mentalis.Security.Ssl;

namespace SharedProtocol.Handshake
{
    public static class HandshakeManager
    {
        public static Action GetHandshakeAction(SecureSocket secureSocket, SecurityOptions options)
        {
            if (options.Protocol == SecureProtocol.None)
            {
                return new UpgradeHandshaker(secureSocket, options.Entity).Handshake;
            }

            return new SecureHandshaker(secureSocket, options).Handshake;
        }
    }
}
