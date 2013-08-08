using System;
using System.Collections.Generic;
using Org.Mentalis.Security.Ssl;

namespace SharedProtocol.Handshake
{
    /// <summary>
    /// Handshake action delegate;
    /// </summary>
    /// <returns></returns>
    public delegate IDictionary<string, object> HandshakeAction();

    /// <summary>
    /// Class chooses which handshake must be performed.
    /// </summary>
    public static class HandshakeManager
    {
        public static HandshakeAction GetHandshakeAction(IDictionary<string, object> handshakeEnvironment)
        {
            if (!handshakeEnvironment.ContainsKey("securityOptions") 
                || !(handshakeEnvironment["securityOptions"] is SecurityOptions))
            {
                throw new ArgumentException("Provide security options for handshake");
            }

            if (!handshakeEnvironment.ContainsKey("secureSocket") 
                || !(handshakeEnvironment["secureSocket"] is SecureSocket))
            {
                throw new ArgumentException("Provide socket for handshake");
            }

            if (!handshakeEnvironment.ContainsKey("end")
            || !(handshakeEnvironment["end"] is ConnectionEnd))
            {
                throw new ArgumentException("Provide connection end for handshake");
            }

            var options = (handshakeEnvironment["securityOptions"] as SecurityOptions);

            if (options.Protocol == SecureProtocol.None)
            {
                //Choose upgrade handshake
                return new UpgradeHandshaker(handshakeEnvironment).Handshake;
            }

            //Choose secure handshake
            return new SecureHandshaker(handshakeEnvironment).Handshake;
        }
    }
}
