namespace Microsoft.Http2.Protocol.Framing
{
    public enum SettingsIds : int
    {
        None = 0,
        MaxConcurrentStreams = 4,
        InitialWindowSize = 7,
        FlowControlOptions = 10
    }
}
