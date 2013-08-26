using Org.Mentalis.Security.Ssl;

namespace SharedProtocol.Handshake
{
    public static class HandshakeExtensions
    {
        public static void MakeSecureHandshake(this SecureSocket socket, SecurityOptions options)
        {
            new SecureHandshaker(socket, options).Handshake();
        }
    }
}
