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

using System.Collections.Generic;
using Org.Mentalis.Security.Ssl;
using System;
using System.Threading;
using SharedProtocol.Exceptions;

namespace SharedProtocol.Handshake
{
    /// <summary>
    /// This class provides secure handshake methods
    /// </summary>
    public sealed class SecureHandshaker : IDisposable
    { 
        private readonly ManualResetEvent _handshakeFinishedEventRaised;

        public SecurityOptions Options { get; private set; }
        public SecureSocket InternalSocket { get; private set; }

        public SecureHandshaker(IDictionary<string, object> handshakeEnvironment) :
            this(
            (SecureSocket) handshakeEnvironment["secureSocket"],
            (SecurityOptions) handshakeEnvironment["securityOptions"])
        {
        }

        public SecureHandshaker(SecureSocket socket, SecurityOptions options)
        {
            InternalSocket = socket;
            InternalSocket.OnHandshakeFinish += HandshakeFinishedHandler;

            Options = options;
            _handshakeFinishedEventRaised = new ManualResetEvent(false);

            if (Options.Protocol == SecureProtocol.None)
            {
                HandshakeFinishedHandler(this, null);
            }
        }

        public IDictionary<string, object> Handshake()
        {
            try
            {
                InternalSocket.StartHandshake();
            }
            catch (Exception)
            {
                throw new Http2HandshakeFailed(HandshakeFailureReason.InternalError);
            }

            _handshakeFinishedEventRaised.WaitOne(8000);
            InternalSocket.OnHandshakeFinish -= HandshakeFinishedHandler;

            if (!InternalSocket.Connected)
            {
                throw new Exception("Connection was lost!");
            }

            if (Options.Protocol != SecureProtocol.None && !InternalSocket.IsNegotiationCompleted)
            {
                throw new Http2HandshakeFailed(HandshakeFailureReason.Timeout);
            }

            return new Dictionary<string, object>();
        }

        private void HandshakeFinishedHandler(object sender, System.EventArgs args)
        {
            _handshakeFinishedEventRaised.Set();
        }

        public void Dispose()
        {
            _handshakeFinishedEventRaised.Dispose();
        }
    }
}
