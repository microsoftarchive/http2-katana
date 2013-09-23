using Org.Mentalis.Security.Ssl;

namespace Org.Mentalis.Security
{
    public static class HandshakeExtensions
    {
        public static void MakeSecureHandshake(this SecureSocket socket, SecurityOptions options)
        {
            new SecureHandshaker(socket, options).Handshake();
        }
    }
}
