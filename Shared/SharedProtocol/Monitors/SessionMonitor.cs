using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedProtocol
{
    public class SessionMonitor : IMonitor
    {
        private Http2Session _monitoringSession;
        private Dictionary<IMonitor, object> _monitorPairs;

        public SessionMonitor(Dictionary<IMonitor, object> monitorPairs)
        {
            _monitorPairs = monitorPairs;
        }

        private void Attach()
        {
            foreach (var monitor in _monitorPairs.Keys)
            {
                monitor.Attach(_monitorPairs[monitor]);
            }
        }

        public void Attach(object sessionForMonitoring)
        {
            if (!(sessionForMonitoring is Http2Session))
                throw new InvalidCastException("Session monitor can be only attached to a Http2Session");

            if (_monitoringSession != null)
                throw new MonitorIsBusyException();

            _monitoringSession = (Http2Session)sessionForMonitoring;

            Attach();
        }

        public void Detach()
        {
            foreach (var monitor in _monitorPairs.Keys)
            {
                monitor.Detach();
            }
        }
    }
}
