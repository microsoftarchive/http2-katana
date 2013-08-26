namespace SharedProtocol.Framing
{
    internal interface IEndStreamFrame
    {
        bool IsEndStream { get; set; }
    }
}
