using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedProtocol.Framing
{
    public enum GoAwayStatusCode : int
    {
        Ok = 0,
        ProtocolError = 1,
        InternalError = 2,
    }
}
