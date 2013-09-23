namespace Microsoft.Http2.Protocol.Framing
{
    public enum ResetStatusCode : uint
    {
        None = 0,
        ProtocolError = 1,
        InternalError = 2,
        FlowControlError = 3,
        UnsuportedVersion = 4,
        StreamClosed = 5,
        FrameTooLarge = 6,
        RefusedStream = 7,
        Cancel = 8,
        CompressionError = 9
    }
}
