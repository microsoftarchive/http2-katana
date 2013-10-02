using Microsoft.Http2.Protocol.Framing;

namespace Microsoft.Http2.Protocol
{
    /// <summary>
    /// This class contains the most commonly used constants
    /// </summary>
    public static class Constants
    {
        // There are always at least 8 bytes in a control frame or data frame
        public const int FramePreambleSize = 8;
        public const int DefaultClientCertVectorSize = 8;
        public const int CurrentProtocolVersion = 6;
        public const int MaxFrameContentSize = 0x3fff; // Spec 06 defines max frame size to be 16383 bytes.
        public const int InitialFlowControlOptionsValue = 0;
        public const string DefaultPath = "/index.html";
        public const string DefaultMethod = Verbs.Get;
        public const string DefaultHost = "localhost";

        //06
        //A sender MUST NOT allow a flow control window to exceed 2^31 - 1 bytes.
        public const int MaxWindowSize = 0x7FFFFFFF;

        //06
        //When a HTTP/2.0 connection is first established, new streams are
        //created with an initial flow control window size of 65535 bytes.  The
        //connection flow control window is 65535 bytes.  
        public const int InitialFlowControlWindowSize = 0xFFFF;
        public const int DefaultStreamPriority = 1 << 30;
        public const string OwinVersion = "1.0";
    }
}
