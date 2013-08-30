namespace Client.Handshake
{
    public enum HandshakeFailureReason : byte
    {
        InternalError = 0,
        Timeout = 1,
    }
}
