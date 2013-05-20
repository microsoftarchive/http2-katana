using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedProtocol
{
    public class SessionMonitor : IMonitor
    {
        private Http2BaseSession _monitoringSession;
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
            if (!(sessionForMonitoring is Http2BaseSession))
                throw new InvalidCastException("Session monitor can be only attached to a Http2BaseSession");

            if (_monitoringSession != null)
                throw new MonitorIsBusyException();

            _monitoringSession = (Http2BaseSession)sessionForMonitoring;

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
