using System;

namespace Microsoft.Http2.Protocol.Exceptions
{
    public class MonitorIsBusyException : Exception
    {
        public MonitorIsBusyException()
            :base("Monitor is busy")
        {
        }
    }
}
