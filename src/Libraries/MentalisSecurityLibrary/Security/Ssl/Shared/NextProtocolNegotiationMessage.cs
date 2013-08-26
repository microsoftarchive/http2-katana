using System;
using System.Text;

namespace Org.Mentalis.Security.Ssl.Shared
{
    using Org.Mentalis;
    using Org.Mentalis.Security.BinaryHelper;

    /// <summary>
    /// This class provides functionality for NPN message, which must be sent after serverHello done receiving if NPN was enabled.
    /// This class is implemented using Singleton pattern
    /// </summary>
	internal sealed class NextProtocolNegotiationMessage : HandshakeMessage
    {
        #region Private Fields

        /// <summary>
        /// The selected protocol during ssl/tls handshake with NPN extenson
        /// </summary>
        private string selectedProtocol;
        /// <summary>
        /// The padding
        /// </summary>
        private byte padding;

        #endregion

        #region Constructors

        /// <summary>
        /// Prevents a default instance of the <see cref="NextProtocolNegotiationMessage"/> class from being created.
        /// </summary>
        /// <param name="type">The message type.</param>
        /// <param name="selectedProtocol">The selected during NPN protocol.</param>
        public NextProtocolNegotiationMessage(HandshakeType type, string selectedProtocol)
            : base(type, null)
        {
            this.selectedProtocol = selectedProtocol;
            this.padding = this.CalcPadding(this.selectedProtocol);
            this.fragment = this.FormByteFragment();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Calcs the padding. See NPN spec for more details
        /// </summary>
        /// <param name="selectedProcotol">The selected during NPN procotol.</param>
        /// <returns> padding </returns>
        private byte CalcPadding(string selectedProcotol)
        {
            return (byte) (32 - ((selectedProcotol.Length + 2) % 32));
        }

        /// <summary>
        /// Initializes HandshakeMessage.fragment byte array.
        /// </summary>
        /// <returns> HandshakeMessage.fragment byte array </returns>
        private byte[] FormByteFragment()
        {
            byte[] tempBuffer = new byte[128];
            int currentLen = 0;

            tempBuffer[currentLen++] = (byte) (this.selectedProtocol.Length);
            
            byte[] protoBytes = Encoding.UTF8.GetBytes(this.selectedProtocol);

            Buffer.BlockCopy(protoBytes, 0, tempBuffer, currentLen, protoBytes.Length);
            currentLen += protoBytes.Length;

            tempBuffer[currentLen++] = this.padding;

            byte[] paddingBytes = new byte[this.padding];
           
            Buffer.BlockCopy(paddingBytes, 0, tempBuffer, currentLen, paddingBytes.Length);
            currentLen += paddingBytes.Length;

            byte[] result = new byte[currentLen];
            Buffer.BlockCopy(tempBuffer, 0, result, 0, currentLen);

            return result;
        }

        #endregion
    }
}
