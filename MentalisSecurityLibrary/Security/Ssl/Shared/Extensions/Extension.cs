//-----------------------------------------------------------------------
// <copyright file="Extension.cs" company="Microsoft Open Technologies, Inc.">
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
using System.IO;

namespace Org.Mentalis.Security.Ssl.Shared.Extensions
{
    /// <summary>
    /// IExtension interface describes basic functionality for all extensions
    /// </summary>
	internal abstract class Extension
    {
        /// <summary>
        /// Gets the type of extension
        /// </summary>
        /// <value>
        /// The extension type.
        /// </value>
        public ExtensionType Type { get; protected set; }

        /// <summary>
        /// Gets the additional info size of extension.
        /// </summary>
        /// <value>
        /// The additional info size of extension.
        /// </value>
        public Int16 ExtensionDataSize { get; protected set; }

        /// <summary>
        /// Gets the total size of the extension.
        /// </summary>
        /// <value>
        /// The total size of the extension.
        /// </value>
        public Int16 ExtensionSize { get; protected set; }

        public virtual void Process(ConnectionEnd end) { }

        public virtual HandshakeMessage GetExtensionResponseMessage() { return null; }

        public virtual void Write(Stream stream, ConnectionEnd end) { }

        public virtual Extension Parse(byte[] serverHello, ref int currentLen, Int16 extLen, ConnectionEnd end) { return this; }

        public virtual Int16 GetExtLength() { return this.ExtensionSize; }
    }
}
