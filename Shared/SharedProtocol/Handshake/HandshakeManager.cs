using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Org.Mentalis.Security.Ssl;

namespace SharedProtocol.Handshake
{
    /// <summary>
    /// Class chooses which handshake must be performed.
    /// </summary>
    public static class HandshakeManager
    {
        public static Action GetHandshakeAction(SecureSocket secureSocket, SecurityOptions options)
        {
            if (options.Protocol == SecureProtocol.None)
            {
                //Choose upgrade handshake
                return new UpgradeHandshaker(secureSocket, options.Entity).Handshake;
            }

            //Choose secure handshake
            return new SecureHandshaker(secureSocket, options).Handshake;
        }
    }
}
