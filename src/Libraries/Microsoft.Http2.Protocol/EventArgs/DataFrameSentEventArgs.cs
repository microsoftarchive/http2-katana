using System;
using Microsoft.Http2.Protocol.Framing;

namespace Microsoft.Http2.Protocol.EventArgs
{
    internal class DataFrameSentEventArgs : System.EventArgs
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
