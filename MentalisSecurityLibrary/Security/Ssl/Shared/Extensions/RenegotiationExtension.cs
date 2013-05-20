//-----------------------------------------------------------------------
// <copyright file="Renegotiation.cs" company="Microsoft Open Technologies, Inc.">
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

namespace Org.Mentalis.Security.Ssl.Shared.Extensions
{
    using Org.Mentalis.Security.BinaryHelper;
    using System.IO;

    /// <summary>
    /// This class provides functionality for Renegotiation extension 
    /// 
    /// Renegotiation extension must be attached if we want to use NPN extension
    /// </summary>
	internal sealed class RenegotiationExtension : Extension
	{
        /// <summary>
        /// The renegotiated connection constant. See Renegotiation spec for more details
        /// </summary>
        private readonly byte renegotiatedConnection = 0;

        /// <summary>
        /// Prevents a default instance of the <see cref="RenegotiationExtension"/> class from being created.
        /// </summary>
        public RenegotiationExtension(ConnectionEnd end)
        {
            this.Type = ExtensionType.Renegotiation;
            this.ExtensionDataSize = 0x01;
            this.ExtensionSize = sizeof(Int16) * 2 + sizeof(byte);
        }

        /// <summary>
        /// Handles the renegotiation extension.
        /// </summary>
        /// <param name="serverHello">The serverHello.</param>
        /// <param name="currentLen">The current index in serverHello array. 
        ///                          This index divides handled and unhandled bytes in serverHello</param>
        /// <param name="extLen">The extension length.</param>
        private void HandleRenegotiationExt(byte[] serverHello, ref int currentLen, Int16 extLen)
        {
            currentLen += extLen; // We are ignoring renegotiation extension
        }

        /// <summary>
        /// Attaches the extension to ClientHello message.
        /// </summary>
        /// <param name="clientHello">The clientHello message.</param>
        /// <param name="currentLen">The current index in clientHello array.
        /// This index divides handled and unhandled bytes in client Hello</param>
        /// <returns>
        /// Byte array that contains clientHello with attached extension
        /// </returns>
        public override void Write(Stream stream, ConnectionEnd end)
        {
            int curPosition = (int)stream.Position;

            byte[] renegotiationType = BinaryHelper.Int16ToBytes((short)this.Type);
            stream.Write(renegotiationType, 0, renegotiationType.Length);

            byte[] extDataSize = BinaryHelper.Int16ToBytes(this.ExtensionDataSize);
            stream.Write(extDataSize, 0, extDataSize.Length);

            stream.WriteByte(this.renegotiatedConnection);

            /*if (this.OnAddedToClientHello != null)
                this.OnAddedToClientHello(this, new AddedToClientHelloArgs(curPosition));*/
        }

        public override Extension Parse(byte[] buffer, ref int currentLen, Int16 extLen, ConnectionEnd end)
        {
            this.HandleRenegotiationExt(buffer, ref currentLen, extLen);

            return this;
        }
	}
}
