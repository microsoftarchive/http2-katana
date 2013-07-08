namespace SharedProtocol.Framing
{
    public enum SettingsIds : int
    {
        None = 0,
        UploadBandwidth = 1,
        DownloadBandwidth = 2,
        RoundTripTime = 3,
        MaxCurrentStreams = 4,
        DownloadRetransRate = 6,
        InitialWindowSize = 7,
        FlowControlOptions = 10
    }
}
