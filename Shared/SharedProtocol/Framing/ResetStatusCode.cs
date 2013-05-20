using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharedProtocol.Framing
{
    public enum ResetStatusCode : uint
    {
        None = 0,
        ProtocolError = 1,
        InvalidStream = 2,
        RefusedStream = 3,
        UnsuportedVersion = 4,
        Cancel = 5,
        InternalError = 6,
        FlowControlError = 7,
        StreamInUse = 8,
        StreamAlreadyClosed = 9,
        InvalidCredentials = 10,
        FrameTooLarge = 11,
    }
}
