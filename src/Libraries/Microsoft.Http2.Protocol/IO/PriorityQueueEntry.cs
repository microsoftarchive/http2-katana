using Microsoft.Http2.Protocol.Framing;

namespace Microsoft.Http2.Protocol.IO
{
    internal class PriorityQueueEntry : IPriorityItem
    {
        private readonly Frame _frame;
        private readonly int _priority;

        public PriorityQueueEntry(Frame frame, int priority)
        {
            _frame = frame;
            _priority = priority;
        }

        public int Priority { get { return _priority; } }

        public Frame Frame { get { return _frame; } }

        public byte[] Buffer { get { return (_frame != null) ? _frame.Buffer : null; } }
    }
}
