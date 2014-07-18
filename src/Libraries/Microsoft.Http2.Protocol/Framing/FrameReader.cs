// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
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
            /* 13 -> 4.1
            Implementations MUST ignore and discard any frame that has a type that is unknown. */
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

                case FrameType.Continuation:
                    return new ContinuationFrame(preamble);

                case FrameType.WindowUpdate:
                    return new WindowUpdateFrame(preamble);

                case FrameType.Data:
                    return new DataFrame(preamble);

                case FrameType.PushPromise:
                    return new PushPromiseFrame(preamble);

                case FrameType.Priority:
                    return new PriorityFrame(preamble);

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
