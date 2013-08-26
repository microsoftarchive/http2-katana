using System;
using SharedProtocol.Framing;

namespace SharedProtocol.Exceptions
{
    /// <summary>
    /// Generic protocol error exception.
    /// </summary>
    public class ProtocolError: Exception
    {
        public ResetStatusCode Code { get; set; }

        public ProtocolError(ResetStatusCode code, string message): base(message)
        {
            Code = code;
        }
    }
}
