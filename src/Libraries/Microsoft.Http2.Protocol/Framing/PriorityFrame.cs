using System.Diagnostics.Contracts;

namespace Microsoft.Http2.Protocol.Framing
{
    /// <summary>
    /// Priority frame class
    /// See spec: http://tools.ietf.org/html/draft-ietf-httpbis-http2-04#section-6.3
    /// </summary>
    internal class PriorityFrame : Frame
    {
        public int Priority
        {
            get { return  FrameHelpers.Get31BitsAt(Buffer, 8); }
            set { FrameHelpers.Set31BitsAt(Buffer, 8, value); }
        }

        public PriorityFrame(int priority, int streamId)
        {
            Contract.Assert(streamId != 0);
            StreamId = streamId;
            Priority = priority;
            FrameType = FrameType.Priority;
        }
    }
}
