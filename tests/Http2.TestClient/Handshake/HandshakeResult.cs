namespace Http2.TestClient.Handshake
{
    public enum HandshakeResult
    {
        None,
        // Successful 101 Switching Protocols response
        Upgrade,
        // Some other status code, fall back to pre-HTTP/2.0 framing
        NonUpgrade,
        // We got back a HTTP/2.0 control frame (presumably a reset frame).
        // The server apparently only understands 2.0 on this port.
        UnexpectedControlFrame,
        // The underlying stream ended gracefully or abortively without completing the handshake.
        UnexpectedConnectionClose,
    }
}
