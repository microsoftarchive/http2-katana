using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedProtocol.Framing
{
    [Flags]
    public enum SettingsFlags : byte
    {
        None = 0x0,
        PresistValue = 0x1,
        Persisted = 0x2,
    }
}
