using System;
using System.Diagnostics.Contracts;

namespace Microsoft.Http2.Protocol.Framing
{
    internal class PushPromiseFrame : Frame, IHeadersFrame
    {
        // The number of bytes in the frame, not including the compressed headers.
        private const byte PreambleSize = 8;
        private const byte PromisedIdOffset = 5;

        public HeadersList Headers { get; set; }

        //EndHeaders and EndPushPromise are 0x04 both
        public bool IsEndHeaders
        {
            get { return IsEndPushPromise; }
            set { IsEndPushPromise = value; }
        }

        public bool IsEndPushPromise
        {
            get
            {
                return (Flags & FrameFlags.EndPushPromise) == FrameFlags.EndPushPromise;
            }
            set
            {
                if (value)
                {
                    Flags |= FrameFlags.EndPushPromise;
                }
            }
        }

        public ArraySegment<byte> CompressedHeaders
        {
            get { return new ArraySegment<byte>(Buffer, PreambleSize, Buffer.Length - PreambleSize); }
        }

        public Int32 PromisedStreamId
        {
            get
            {
                return FrameHelpers.Get31BitsAt(Buffer, PromisedIdOffset);
            }
            set
            {
                Contract.Assert(value >= 0 && value <= 255);
                FrameHelpers.Set31BitsAt(Buffer, PromisedIdOffset, value);
            }
        }

        public PushPromiseFrame(Frame preamble)
            : base(preamble)
        {
        }

        public PushPromiseFrame(Int32 streamId, Int32 promisedStreamId,
                               bool isEndPushPromise, HeadersList headers = null)
        {
            Contract.Assert(streamId > 0 && promisedStreamId > 0);
            StreamId = streamId;
            PromisedStreamId = promisedStreamId;
            Headers = headers ?? new HeadersList();
            IsEndPushPromise = isEndPushPromise;
        }
    }
}
