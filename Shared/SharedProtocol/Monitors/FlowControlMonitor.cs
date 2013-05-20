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
    public class FlowControlMonitor : IMonitor
    {
        private WriteQueue _monitoringObject;

        private Action<object, FrameSentEventArgs> _handler;

        public FlowControlMonitor(Action<object, FrameSentEventArgs> handler)
        {
            _handler = handler;
        }

        public void Attach(object forMonitoring)
        {
            if (_monitoringObject != null)
                throw new MonitorIsBusyException();

            if (!(forMonitoring is WriteQueue))
                throw new InvalidCastException("Flow control monitor can be attached only to a WriteQueue object");

            _monitoringObject = (WriteQueue)forMonitoring;
            _monitoringObject.OnFrameSent += FrameSentHandler;
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
