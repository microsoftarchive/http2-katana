// -----------------------------------------------------------------------
// <copyright file="SecureHandshaker.cs" company="Microsoft">
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

using System.Net;
using System.Net.Sockets;
using Org.Mentalis.Security.Certificates;
using Org.Mentalis.Security.Ssl.Shared.Extensions;
using Org.Mentalis.Security.Ssl;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Org.Mentalis
{
    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public sealed class SecureHandshaker : IDisposable
    { 
        private ALPNExtensionMonitor monitor = null;
        private readonly ExtensionType[] extensionTypes = null;
        private ManualResetEvent handshakeFinishedEventRaised;
        private Uri uri;

        public SecurityOptions Options { get; private set; }
        public string SelectedProtocol { get; private set; }
        public SecureSocket InternalSocket { get; private set; }

        public SecureHandshaker(SecurityOptions options, Uri uri)
        {
            this.Options = options;
            this.uri = uri;
            this.monitor = new ALPNExtensionMonitor();
            this.handshakeFinishedEventRaised = new ManualResetEvent(false);
        }

        public SecureHandshaker(Uri uri, IEnumerable<ExtensionType> extensionTypes = null)
        {
            this.uri = uri;

            if (extensionTypes == null)
                this.extensionTypes = new ExtensionType[] {ExtensionType.Renegotiation, ExtensionType.ALPN};
            else
                this.extensionTypes = (ExtensionType[]) extensionTypes;

            this.Options = new SecurityOptions(SecureProtocol.Tls1, this.extensionTypes, ConnectionEnd.Client);

            this.Options.Entity = ConnectionEnd.Client;
            this.Options.CommonName = uri.Host;
            this.Options.VerificationType = CredentialVerification.None;
            this.Options.Certificate = Certificate.CreateFromCerFile(@"certificate.pfx");
            this.Options.Flags = SecurityFlags.Default;
            this.Options.AllowedAlgorithms = SslAlgorithms.RSA_AES_128_SHA | SslAlgorithms.NULL_COMPRESSION;
            
            this.monitor = new ALPNExtensionMonitor();
            this.handshakeFinishedEventRaised = new ManualResetEvent(false);
        }

        public void Handshake()
        {
            this.InternalSocket = new SecureSocket(AddressFamily.InterNetwork, SocketType.Stream, 
                                                        ProtocolType.Tcp, this.Options);

            this.InternalSocket.OnHandshakeFinish += this.HandshakeFinishedHandler;
            this.monitor.OnProtocolSelected += this.ProtocolSelectedHandler;

            this.InternalSocket.Connect(new DnsEndPoint(this.uri.Host, this.uri.Port), this.monitor);

            this.handshakeFinishedEventRaised.WaitOne(60000);

            if (!this.InternalSocket.IsNegotiationCompleted)
            {
                throw new Exception("Handshake failed");
            }

            if (!this.InternalSocket.Connected)
            {
                throw new Exception("Connection was lost!");
            }

            this.InternalSocket.OnHandshakeFinish -= this.HandshakeFinishedHandler;
        }

        private void ProtocolSelectedHandler(object sender, ProtocolSelectedArgs args)
        {
            this.SelectedProtocol = args.SelectedProtocol;
        }

        private void HandshakeFinishedHandler(object sender, EventArgs args)
        {
            this.handshakeFinishedEventRaised.Set();
        }

        public void Dispose()
        {
            //this.InternalSocket.Close();
            this.handshakeFinishedEventRaised.Dispose();
            this.monitor.Dispose();
        }
    }
}
