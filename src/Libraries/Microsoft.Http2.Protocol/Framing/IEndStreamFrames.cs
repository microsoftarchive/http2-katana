namespace Microsoft.Http2.Protocol.Framing
{
    internal interface IEndStreamFrame
    {
        bool IsEndStream { get; set; }
    }
}
