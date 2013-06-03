using System;
using SharedProtocol.Framing;

namespace SharedProtocol
{
    public class SettingsSentEventArgs : EventArgs
    {
        public SettingsFrame SettingsFrame { get; private set; }
        
        public SettingsSentEventArgs(SettingsFrame frame)
        {
            SettingsFrame = frame;
        }
    }
}
