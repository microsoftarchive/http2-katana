namespace SharedProtocol.Handshake
{
    public enum HandshakeFailureReason : byte
    {
        InternalError = 0,
        Timeout = 1,
    }
}
