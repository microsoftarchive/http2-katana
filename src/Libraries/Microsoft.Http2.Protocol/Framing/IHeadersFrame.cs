namespace Microsoft.Http2.Protocol.Framing
{
    internal interface IHeadersFrame
    {
        HeadersList Headers { get; }
        bool IsEndHeaders { get; set; }
    }
}
