namespace Microsoft.Http2.Protocol
{
    /// <summary>
    /// This class contains the most commonly used constants
    /// </summary>
    public class Constants
    {
        // There are always at least 8 bytes in a control frame or data frame
        public const int FramePreambleSize = 8;
        public const int DefaultClientCertVectorSize = 8;
        public const int CurrentProtocolVersion = 3;
        public const int DefaultFlowControlCredit = 0xFFFF; // 64kb
        public const int MaxDataFrameContentSize = 0x3fff; // Spec 06 defines max frame size to be 16383 bytes.
        public const int InitialFlowControlOptionsValue = 0;

        public const string OwinVersion = "1.0";
    }
}
