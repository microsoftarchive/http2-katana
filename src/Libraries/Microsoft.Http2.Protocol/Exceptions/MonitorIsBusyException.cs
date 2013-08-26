using System;

namespace SharedProtocol.Exceptions
{
    public class MonitorIsBusyException : Exception
    {
        public MonitorIsBusyException()
            :base("Monitor is busy")
        {
        }
    }
}
