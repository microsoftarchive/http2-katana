//-----------------------------------------------------------------------
// <copyright file="NextProtocolNegotiationExtension.cs" company="Microsoft Open Technologies, Inc.">
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
using System.Collections.Generic;
using System.Text;

namespace Org.Mentalis.Security.Ssl.Shared.Extensions
{
    using Org.Mentalis.Security.BinaryHelper;
    using Org.Mentalis.Security.Ssl.Shared.Extensions.ExtensionMessages;
    using System.IO;

    /// <summary>
    /// This class provides functionality for NPN extension 
    /// </summary>
	internal class NextProtocolNegotiationExtension : Extension, IProtocolSelectionExtension
    {
        #region Constructors
        /// <summary>
        /// Prevents a default instance of the <see cref="NextProtocolNegotiationExtension"/> class from being created.
        /// </summary>
        public NextProtocolNegotiationExtension(ConnectionEnd end)
        {
            this.Type = ExtensionType.NextNegotiation;
            string[] protocols = new string[3] { "spdy/3", "spdy/2", "http/1.1" };

            if (end == ConnectionEnd.Client)
            {
                this.ExtensionDataSize = 0x00; // must be zero. See NPN spec for more details
                this.ExtensionSize = sizeof(Int16) * 2;

                this.ClientKnownProtocolList = new List<string>(protocols);
                this.ServerKnownProtocolList = null;
            }
            else
            {
                this.ExtensionSize = sizeof(Int16);
                this.ExtensionDataSize = 0;
                Int16 prefixLen = sizeof(Int16);
                this.ServerKnownProtocolList = new List<string>(protocols);

                foreach (var protocol in this.ServerKnownProtocolList)
                    this.ExtensionDataSize += (Int16) (prefixLen + protocol.Length);

                this.ExtensionSize += this.ExtensionDataSize;

                this.ClientKnownProtocolList = null;
            }
        }
        #endregion

        #region Public properties
        /// <summary>
        /// Gets or sets the client known protocols list.
        /// </summary>
        /// <value>
        /// The client known protocols list.
        /// </value>
        public List<string> ClientKnownProtocolList { get; private set; }

        /// <summary>
        /// Gets or sets the server known protocol list.
        /// </summary>
        /// <value>
        /// The server known protocol list.
        /// </value>
        public List<string> ServerKnownProtocolList { get; private set; }

        /// <summary>
        /// Gets or sets the selected during NPN protocol.
        /// </summary>
        /// <value>
        /// The selected protocol.
        /// </value>
        public string SelectedProtocol { get; set; }

        #endregion

        #region Private Methods

        private List<string> ParseNpnServerKnownProtocols(byte[] buffer, ref int currentLen, Int16 extLen)
        {
            var protocolList = new List<string>(10);
            int serverHelloBeginPosition = currentLen;

            while (currentLen < serverHelloBeginPosition + extLen)
            {
                byte len = buffer[currentLen++];
                byte[] serializedProtocolName = new byte[len];

                Buffer.BlockCopy(buffer, currentLen, serializedProtocolName, 0, len);
                currentLen += len;

                protocolList.Add(Encoding.UTF8.GetString(serializedProtocolName));
            }

            return protocolList;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Selects the protocol.
        /// </summary>
        /// <exception cref="System.NullReferenceException">ServerKnownProtocolList was not initialized!</exception>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">There are no protocol known both by client and server!</exception>
        public override void Process(ConnectionEnd end)
        {
            if (end == ConnectionEnd.Client)
            {
                if (this.ServerKnownProtocolList == null)
                    throw new NullReferenceException("ServerKnownProtocolList was not initialized!");

                foreach (var protocol in this.ClientKnownProtocolList)
                {
                    if (this.ServerKnownProtocolList.Contains(protocol))
                    {
                        this.SelectedProtocol = protocol;

                        if (this.OnProtocolSelected != null)
                            this.OnProtocolSelected(this, new ProtocolSelectedArgs(this.SelectedProtocol));

                        return;
                    }
                }

                throw new KeyNotFoundException("There are no protocol known both by client and server!");
            }
        }

        #region Inherited methods
        /// <summary>
        /// Gets the total length of the extension.
        /// </summary>
        /// <returns>
        /// Length of extension
        /// </returns>
        public override Int16 GetExtLength() { return this.ExtensionSize; }

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
            int extBeginPos = (int)stream.Position;

            if (end == ConnectionEnd.Client)
            {
                byte[] nextProtoType = BinaryHelper.Int16ToBytes((short)this.Type);
                stream.Write(nextProtoType, 0, nextProtoType.Length);

                byte[] extDataSize = BinaryHelper.Int16ToBytes(this.ExtensionDataSize);
                stream.Write(extDataSize, 0, extDataSize.Length);

                if (this.OnAddedToClientHello != null)
                    this.OnAddedToClientHello(this, new NPNAddedToClientHelloArgs(extBeginPos));
            }
            else
            {
                byte[] nextProtoType = BinaryHelper.Int16ToBytes((short)this.Type);
                stream.Write(nextProtoType, 0, nextProtoType.Length);

                this.ExtensionSize = sizeof(Int16);
                foreach (var protocol in this.ServerKnownProtocolList)
                {
                    this.ExtensionSize += (Int16)(protocol.Length + sizeof(Int16));
                }

                byte[] extLenBytes = BinaryHelper.Int16ToBytes(this.ExtensionDataSize);
                stream.Write(extLenBytes, 0, extLenBytes.Length);

                this.ExtensionDataSize = (Int16) (this.ExtensionSize - sizeof(Int16));
                byte[] listLenBytes = BinaryHelper.Int16ToBytes(this.ExtensionDataSize);
                stream.Write(listLenBytes, 0, listLenBytes.Length);

                foreach (var protocol in this.ServerKnownProtocolList)
                {
                    byte[] protoPrefixBytes = BinaryHelper.Int16ToBytes((Int16) protocol.Length);
                    stream.Write(protoPrefixBytes, 0, protoPrefixBytes.Length);

                    byte[] protoBytes = Encoding.UTF8.GetBytes(protocol);
                    stream.Write(protoBytes, 0, protoBytes.Length);
                }
            }
        }

        public override Extension Parse(byte[] buffer, ref int currentLen, Int16 extLen, ConnectionEnd end)
        {
            int extBeginPos = currentLen;

            if (end == ConnectionEnd.Client)
            {
                List<string> serverKnownProtocolList = this.ParseNpnServerKnownProtocols(buffer, ref currentLen, extLen);
                this.ServerKnownProtocolList = serverKnownProtocolList;

                if (this.OnParsedFromServerHello != null)
                    this.OnParsedFromServerHello(this, new NPNParsedFromServerHelloArgs(extBeginPos));
            }
            else
            {
                if (extLen > 0)
                    this.ExtensionDataSize = BinaryHelper.Int16FromBytes(buffer[currentLen++], buffer[currentLen++]);
            }
            return this;
        }

        public override HandshakeMessage GetExtensionResponseMessage()
        {
            var msg = new NextProtocolNegotiationMessage(HandshakeType.NextProtocolNegotiation, this.SelectedProtocol);
            return msg;
        }
        #endregion
        #endregion

        public event EventHandler<NPNAddedToClientHelloArgs> OnAddedToClientHello;
        public event EventHandler<NPNParsedFromServerHelloArgs> OnParsedFromServerHello;
        public event EventHandler<ProtocolSelectedArgs> OnProtocolSelected;
    }
}
