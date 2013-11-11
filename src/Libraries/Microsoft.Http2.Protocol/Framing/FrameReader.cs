using System;
using System.IO;

namespace Microsoft.Http2.Protocol.Framing
{
    /// <summary>
    /// This class reads frames and gets their type
    /// </summary>
    internal class FrameReader : IDisposable
    {
        private readonly Stream _stream;
        private bool _isDisposed;

        public FrameReader(Stream stream)
        {
            _stream = stream;
            _isDisposed = false;
        }

        public Frame ReadFrame()
        {
            if (_isDisposed)
                return null;

            var preamble = new Frame();
            if (!TryFill(preamble.Buffer, 0, preamble.Buffer.Length))
            {
                return null;
            }

            Frame wholeFrame;
            try
            {
                wholeFrame = GetFrameType(preamble);
            }
            //Item 4.1 in 06 spec: Implementations MUST ignore frames of unsupported or unrecognized types
            catch (NotImplementedException)
            {
                return preamble;
            }
            
            if (!TryFill(wholeFrame.Buffer, Constants.FramePreambleSize, wholeFrame.Buffer.Length - Constants.FramePreambleSize))
            {
                return null;
            }

            return wholeFrame;
        }

        private static Frame GetFrameType(Frame preamble)
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
            while (totalRead < count && !_isDisposed)
            {
                int read =_stream.Read(buffer, offset + totalRead, count - totalRead);

                if (read <= 0)
                {
                    //The stream ended before we could get as much as we needed.
                   return false;
                }
                totalRead += read;
            }

            return true;
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
        }
    }
}
