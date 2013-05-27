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

        public FrameReader(SecureSocket socket)
        {
            _socket = socket;
        }

        public Frame ReadFrame()
        {
            Frame preamble = new Frame();
            if (!TryFill(preamble.Buffer, 0, preamble.Buffer.Length))
            {
                return null;
            }

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
                    var dataFrame = new DataFrame(preamble);
                    //if (this.OnDataFrameReceived != null)
                    //{
                    //    this.OnDataFrameReceived(this, new DataFrameReceivedEventArgs(dataFrame));
                    //}

                    return dataFrame;

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
