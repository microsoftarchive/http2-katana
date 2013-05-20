using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Org.Mentalis.Security.Ssl;

namespace SharedProtocol.Framing
{
    public class FrameReader
    {
        private SecureSocket _socket;
        private bool _validateFirstFrameIsControl;

        public FrameReader(SecureSocket socket, bool validateFirstFrameIsControl)
        {
            _socket = socket;
            _validateFirstFrameIsControl = validateFirstFrameIsControl;
        }

        public Frame ReadFrame()
        {
            Frame preamble = new Frame();
            if (!TryFill(preamble.Buffer, 0, preamble.Buffer.Length))
            {
                return null;
            }

            if (_validateFirstFrameIsControl)
            {
                if (!preamble.IsControl)
                {
                    // Probably a HTTP/1.1 text formatted request.  We could check if it starts with 'GET'
                    // Is it sane to send a response here?  What kind of response? 1.1 text, or 2.0 binary?
                    throw new ProtocolViolationException("First frame is not a control frame.");
                }
                _validateFirstFrameIsControl = false;
            }
            // TODO: If this is the first frame, verify that it is in fact a control frame, and that it is not a HTTP/1.1 text request.
            // Not applicable after an HTTP/1.1->HTTP-01/2.0 upgrade handshake.

            Frame wholeFrame = GetFrameType(preamble);
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

                case FrameType.Headers:
                    return new HeadersFrame(preamble);

                case FrameType.Ping:
                    return new PingFrame(preamble);

                case FrameType.RstStream:
                    return new RstStreamFrame(preamble);

                case FrameType.Settings:
                    return new SettingsFrame(preamble);

                case FrameType.HeadersPlusPriority:
                    return new HeadersPlusPriority(preamble);

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
                // TODO: Over-read into a buffer to reduce the number of native read operations.
                int read = _socket.Receive(buffer, offset + totalRead, count - totalRead, SocketFlags.None);
                if (read <= 0)
                {
                    // The stream ended before we could get as much as we needed.
                    return false;
                }
                totalRead += read;
            }
            return true;
        }
    }
}
