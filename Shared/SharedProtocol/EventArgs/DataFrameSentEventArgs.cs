using System;
using SharedProtocol.Framing;

namespace SharedProtocol
{
    public class DataFrameSentEventArgs : EventArgs
    {
        public Int32 DataAmount{ get; private set; }
        public Int32 Id { get; private set; }

        public DataFrameSentEventArgs(Frame frame)
        {
            this.Id = frame.StreamId;
            this.DataAmount = frame.Buffer.Length - Constants.FramePreambleSize;
        }
    }
}
