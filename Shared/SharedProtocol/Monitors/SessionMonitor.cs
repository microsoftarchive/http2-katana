using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedProtocol.IO;

namespace SharedProtocol
{
    public class SessionMonitor : IMonitor
    {
        private Http2BaseSession _monitoringSession;
        private WriteQueue _monitoringObject;

        private delegate void Something(object sender, EventArgs args);

        private Action<object, EventArgs> _handler;

        public SessionMonitor(Http2BaseSession session, Action<object, EventArgs> handler)
        {
            _monitoringSession = session;
            _handler = handler;
        }

        public void Attach(IMonitorable forMonitoring)
        {
            if (_monitoringObject != null)
                throw new MonitorIsBusyException();

            //_monitoringObject += FrameSentHandler;
        }

        public void Detach()
        {
            if (_monitoringObject == null)
                throw new NoNullAllowedException("You are called Detach for non-attached monitor!");
        }

        private void FrameSentHandler(object sender, FrameSentEventArgs args)
        {
            _handler.Invoke(sender, args);
        }
    }
}
