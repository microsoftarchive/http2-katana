using System;

namespace SharedProtocol
{
    public class MonitorIsBusyException : Exception
    {
        public MonitorIsBusyException()
            :base("Monitor is busy")
        {
        }
    }
}
