using SharedProtocol.Framing;

namespace SharedProtocol.IO
{
    internal class PriorityQueueEntry : IPriorityItem
    {
        private readonly Frame _frame;
        private readonly Priority _priority;

        public PriorityQueueEntry(Frame frame, Priority priority)
        {
            _frame = frame;
            _priority = priority;
        }

        public Priority Priority { get { return _priority; } }

        public Frame Frame { get { return _frame; } }

        public byte[] Buffer { get { return (_frame != null) ? _frame.Buffer : null; } }
    }
}
