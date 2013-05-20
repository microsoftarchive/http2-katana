//-----------------------------------------------------------------------
// <copyright file="NextProtocolNegotiationMessage.cs" company="Microsoft Open Technologies, Inc.">
//Copyright © 2002-2007, The Mentalis.org Team
//Portions Copyright © Microsoft Open Technologies, Inc.
//All rights reserved.
//http://www.mentalis.org/ 
//Redistribution and use in source and binary forms, with or without modification, 
//are permitted provided that the following conditions are met:
//- Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer. 
//- Neither the name of the Mentalis.org Team, 
//nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
//THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, 
//INCLUDING, BUT NOT LIMITED TO, 
//THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. 
//IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, 
//INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, 
//PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; 
//OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
//OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, 
//EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Text;

namespace Org.Mentalis.Security.Ssl.Shared.Extensions.ExtensionMessages
{
    /// <summary>
    /// This class provides functionality for NPN message, which must be sent after serverHello done receiving if NPN was enabled.
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
