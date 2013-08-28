using System;
using SharedProtocol.Framing;

namespace SharedProtocol.EventArgs
{
    /// <summary>
    /// This class is designed for future usage
    /// </summary>
    internal class DataFrameReceivedEventArgs : System.EventArgs
    {
        public Int32 DataAmount { get; private set; }
        public Int32 Id { get; private set; }

        public DataFrameReceivedEventArgs(DataFrame frame)
        {
            Id = frame.StreamId;
            DataAmount = frame.Buffer.Length - Constants.FramePreambleSize;
        }
    }
}
