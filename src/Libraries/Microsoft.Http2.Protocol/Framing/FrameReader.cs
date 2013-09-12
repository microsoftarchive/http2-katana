using System;
using Microsoft.Http2.Protocol.IO;

namespace Microsoft.Http2.Protocol.Framing
{
    /// <summary>
    /// This class reads frames and gets their type
    /// </summary>
    internal class FrameReader
    {
        private readonly DuplexStream _stream;

        public FrameReader(DuplexStream stream)
        {
            _stream = stream;
        }

        public Frame ReadFrame()
        {
            var preamble = new Frame();
            if (!TryFill(preamble.Buffer, 0, preamble.Buffer.Length))
            {
                return null;
            }

            var wholeFrame = GetFrameType(preamble);
            if (!TryFill(wholeFrame.Buffer, Constants.FramePreambleSize, wholeFrame.Buffer.Length - Constants.FramePreambleSize))
            {
                return null;
            }

            return wholeFrame;
        }

        private Frame GetFrameType(Frame preamble)
        {
            switch (preamble.FrameType)
            {
                case FrameType.GoAway:
                    return new GoAwayFrame(preamble);

                case FrameType.Ping:
                    return new PingFrame(preamble);

                case FrameType.RstStream:
                    return new RstStreamFrame(preamble);

                case FrameType.Settings:
                    return new SettingsFrame(preamble);

                case FrameType.Headers:
                    return new HeadersFrame(preamble);

                case FrameType.WindowUpdate:
                    return new WindowUpdateFrame(preamble);

                case FrameType.Data:
                    return new DataFrame(preamble);
                default:
                    throw new NotImplementedException("Frame type: " + preamble.FrameType);
            }
        }

        private bool TryFill(byte[] buffer, int offset, int count)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                // Infinite wait
                if (!_stream.WaitForDataAvailable(-1)) {
                    // stream closed
                    return false;
                }
                // TODO: Over-read into a buffer to reduce the number of native read operations.
                int read = _stream.Read(buffer, offset + totalRead, count - totalRead);

                if (read <= 0)
                {
                    //The stream ended before we could get as much as we needed.
                    return false;
                }
                totalRead += read;
            }
            return true;
        }
    }
}
