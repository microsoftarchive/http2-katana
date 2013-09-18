namespace Http2.TestClient.Handshake
{
    public enum HandshakeFailureReason : byte
    {
        InternalError = 0,
        Timeout = 1,
    }
}
