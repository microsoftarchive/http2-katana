using System;

namespace Microsoft.Http2.Protocol.Framing
{
    [Flags]
    public enum SettingsFlags : byte
    {
        None = 0x0,
        PresistValue = 0x1,
        Persisted = 0x2,
    }
}
