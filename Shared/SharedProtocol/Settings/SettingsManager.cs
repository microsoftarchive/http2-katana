using SharedProtocol.Framing;

namespace SharedProtocol
{
    /// <summary>
    /// This class is designed for incoming settings frame processing
    /// </summary>
    internal class SettingsManager 
    {
        public void ProcessSettings(SettingsFrame settingsFrame, Http2Session session, 
                                        FlowControlManager flCtrlManager)
        {
            for (int i = 0; i < settingsFrame.EntryCount; i++)
            {

                switch (settingsFrame[i].Id)
                {
                    case SettingsIds.UploadBandwidth:
                        break;
                    case SettingsIds.DownloadBandwidth:
                        break;
                    case SettingsIds.RoundTripTime:
                        break;
                    case SettingsIds.MaxCurrentStreams:
                        session.RemoteMaxConcurrentStreams = settingsFrame[i].Value;
                        break;
                    case SettingsIds.DownloadRetransRate:
                        break;
                    case SettingsIds.InitialWindowSize:
                        flCtrlManager.StreamsInitialWindowSize = settingsFrame[i].Value;
                        break;
                    case SettingsIds.FlowControlOptions:
                        flCtrlManager.Options = settingsFrame[i].Value;
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
