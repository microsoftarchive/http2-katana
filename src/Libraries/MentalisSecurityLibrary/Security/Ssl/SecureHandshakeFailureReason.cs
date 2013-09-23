namespace Security.Ssl
{
    public enum SecureHandshakeFailureReason : int
    {
        None = 0,
        HandshakeInternalError = 1,
        HandshakeTimeout = 2
    }
}
