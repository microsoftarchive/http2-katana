using System;
using Microsoft.Http2.Protocol.FlowControl;
using Microsoft.Http2.Protocol.Framing;

namespace Microsoft.Http2.Protocol.Settings
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
                    case SettingsIds.MaxConcurrentStreams:
                        session.RemoteMaxConcurrentStreams = settingsFrame[i].Value;
                        break;
                    case SettingsIds.InitialWindowSize:
                        int newInitWindowSize = settingsFrame[i].Value;
                        int windowSizeDiff = newInitWindowSize - flCtrlManager.StreamsInitialWindowSize;

                        foreach (var stream in session.ActiveStreams.FlowControlledStreams.Values)
                        {
                            stream.WindowSize += windowSizeDiff;
                        }

                        flCtrlManager.StreamsInitialWindowSize = newInitWindowSize;
                        session.InitialWindowSize = newInitWindowSize;
                        break;
                    case SettingsIds.FlowControlOptions:
                        flCtrlManager.Options = settingsFrame[i].Value;
                        break;
                }
            }
        }
    }
}
