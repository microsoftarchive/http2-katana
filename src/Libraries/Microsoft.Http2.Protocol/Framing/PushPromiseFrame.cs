using System;
using System.Diagnostics.Contracts;

namespace Microsoft.Http2.Protocol.Framing
{
    internal class PushPromiseFrame : Frame, IHeadersFrame
    {
        // The number of bytes in the frame, not including the compressed headers.
        private const byte PreambleSize = 8;
        private const byte PromisedIdOffset = 8;

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
            get
            {
                //preamble (8 bytes) -> promised Id (4 bytes) -> compressed headers
                var compressedBytesPayload = Buffer.Length - PreambleSize - sizeof(Int32);
                Contract.Assert(compressedBytesPayload >= 0);

                return new ArraySegment<byte>(Buffer, PreambleSize, compressedBytesPayload);
            }
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
            :base(new byte[12])
        {
            Contract.Assert(streamId > 0 && promisedStreamId > 0);
            StreamId = streamId;
            FrameType = FrameType.PushPromise;
            FrameLength = Buffer.Length - Constants.FramePreambleSize;
            PromisedStreamId = promisedStreamId;
            Headers = headers ?? new HeadersList();
            IsEndPushPromise = isEndPushPromise;
        }
    }
}
