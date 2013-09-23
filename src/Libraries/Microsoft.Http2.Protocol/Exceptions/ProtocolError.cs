using System;
using Microsoft.Http2.Protocol.Framing;

namespace Microsoft.Http2.Protocol.Exceptions
{
    /// <summary>
    /// Generic protocol error exception.
    /// </summary>
    internal class ProtocolError : Exception
    {
        public ResetStatusCode Code { get; set; }

        public ProtocolError(ResetStatusCode code, string message): base(message)
        {
            Code = code;
        }
    }
}
