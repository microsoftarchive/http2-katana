using System;
using SharedProtocol.Framing;

namespace SharedProtocol.EventArgs
{
    public class DataFrameSentEventArgs : System.EventArgs
    {
        public Int32 DataAmount{ get; private set; }
        public Int32 Id { get; private set; }

        public DataFrameSentEventArgs(Frame frame)
        {
            Id = frame.StreamId;
            DataAmount = frame.Buffer.Length - Constants.FramePreambleSize;
        }
    }
}
