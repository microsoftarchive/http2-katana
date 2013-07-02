//-----------------------------------------------------------------------
// <copyright file="ALPNExtension.cs" company="Microsoft Open Technologies, Inc.">
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
using System.Linq;

namespace Org.Mentalis.Security.Ssl.Shared.Extensions
{
    using Org.Mentalis;
    using Org.Mentalis.Security.BinaryHelper;
    using System.IO;

    internal sealed class ALPNExtension : Extension, IProtocolSelectionExtension
	{
        private string selectedProtocol;

        public List<string> ClientKnownProtocolList { get; private set; }
        public List<string> ServerKnownProtocolList { get; private set; }

        public string SelectedProtocol
        {
            get
            {
                return this.selectedProtocol;
            }
            set
            {
                this.selectedProtocol = value;

                if (this.OnProtocolSelected != null)
                    this.OnProtocolSelected(this, new ProtocolSelectedArgs(this.SelectedProtocol));
            }
         }

        public ALPNExtension(ConnectionEnd end, IEnumerable<string> knownProtocols)
        {
            this.Type = ExtensionType.ALPN;

            this.SelectedProtocol = String.Empty;
            if (end == ConnectionEnd.Client)
            {
                this.ClientKnownProtocolList = new List<string>(knownProtocols);
                this.ExtensionDataSize = CalcExtensionDataSize(this.ClientKnownProtocolList);
            }
            else
            {
                this.ServerKnownProtocolList = new List<string>(knownProtocols);
            }

            this.ExtensionSize = (Int16)(sizeof(Int16) * 2 + this.ExtensionDataSize); //type + length + data length
        }

        private Int16 CalcExtensionDataSize(List<string> protocolList)
        {
            Int16 result = 0;
            byte lenPrefix = sizeof(byte);
            //byte lenPrefix = sizeof(Int16);  // this is correct. Every length field is 2 bytes

            foreach (var protocol in protocolList)
                result += (Int16) (protocol.Length + lenPrefix);

            return result;
        }

        public override Extension Parse(byte[] buffer, ref int currentLen, Int16 extLen, ConnectionEnd ep)
        {
            int extBeginPos = currentLen;

            Int16 protocolListFullLength = BinaryHelper.Int16FromBytes(buffer[currentLen++], buffer[currentLen++]);

            if (ep == ConnectionEnd.Client)
            {
                byte protoLen = buffer[currentLen++];

                this.SelectedProtocol = Encoding.UTF8.GetString(buffer, currentLen, protoLen);
                currentLen += extLen;

                if (this.OnParsedFromServerHello != null)
                    this.OnParsedFromServerHello(this, new ALPNParsedFromServerHelloArgs(extBeginPos));

            }
            else
            {
                this.ClientKnownProtocolList = new List<string>(3);
                while (currentLen < extBeginPos + extLen)
                {
                    byte protoLen = buffer[currentLen++];
                    string protocol = Encoding.UTF8.GetString(buffer, currentLen, protoLen);
                    currentLen += protoLen;

                    this.ClientKnownProtocolList.Add(protocol);
                }
            }
            return this;
        }

        public override void Process(ConnectionEnd end)
        {
            if (end == ConnectionEnd.Server)
            {
                foreach (var protocol in this.ClientKnownProtocolList)
                {
                    if (this.ServerKnownProtocolList.Contains(protocol))
                    {
                        this.SelectedProtocol = protocol;
                        this.ExtensionDataSize = (short) (Encoding.UTF8.GetByteCount(this.SelectedProtocol) + sizeof(byte));
                        this.ExtensionSize += ExtensionDataSize;
                        break;
                    }
                }
                if (this.SelectedProtocol == String.Empty)
                {
                    throw new ALPNCantSelectProtocolException();
                }
            }
        }

        public override Int16 GetExtLength() { return this.ExtensionSize; }

        public override void Write(Stream stream, ConnectionEnd end)
        {
            int curPosition = (int)stream.Position;

            byte[] alpnType = BinaryHelper.Int16ToBytes((short)this.Type);
            stream.Write(alpnType, 0, alpnType.Length);

            byte[] extDataSize = BinaryHelper.Int16ToBytes(this.ExtensionDataSize);
            stream.Write(extDataSize, 0, extDataSize.Length);

            if (end == ConnectionEnd.Client)
            {
                Int16 serializedProtocolListSize = 0;

                this.ClientKnownProtocolList.Count(protocol =>
                    {
                        serializedProtocolListSize += (Int16) (protocol.Length + 1);
                        return true;
                    });
                byte[] serializedProtocolListSizeBytes = BinaryHelper.Int16ToBytes(serializedProtocolListSize);
                stream.Write(serializedProtocolListSizeBytes, 0, serializedProtocolListSizeBytes.Length);

                foreach (var protocol in this.ClientKnownProtocolList)
                {
                    byte protoLen = (byte) (protocol.Length);
                    stream.WriteByte(protoLen);

					// every length field is 2 bytes
					// 
                    //byte[] protoLen = BinaryHelper.Int16ToBytes((Int16)protocol.Length);
                    //stream.Write(protoLen, 0, protoLen.Length);

                    byte[] protocolData = Encoding.UTF8.GetBytes(protocol);
                    stream.Write(protocolData, 0, protocolData.Length);
                }

                if (this.OnAddedToClientHello != null)
                    this.OnAddedToClientHello(this, new ALPNAddedToClientHelloArgs(curPosition));
            }
            else
            {
                byte selectedProtoLen = (byte) (Encoding.UTF8.GetByteCount(this.SelectedProtocol));
                byte[] selectedProtoBytes = Encoding.UTF8.GetBytes(this.SelectedProtocol);

                stream.WriteByte(selectedProtoLen);
                stream.Write(selectedProtoBytes, 0, selectedProtoBytes.Length);
            }
        }

        public event EventHandler<ALPNAddedToClientHelloArgs> OnAddedToClientHello;
        public event EventHandler<ALPNParsedFromServerHelloArgs> OnParsedFromServerHello;
        public event EventHandler<ProtocolSelectedArgs> OnProtocolSelected;
	}
}