// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
using System.IO;
using Microsoft.Http2.Protocol.Compression;
using Microsoft.Http2.Protocol.Framing;
using System;
using System.Threading;
using Microsoft.Http2.Protocol.Utils;

namespace Microsoft.Http2.Protocol.IO
{
    // Queue up frames to send, including headers, body, flush, pings, etc.
    internal sealed class WriteQueue : IDisposable
    {
        private readonly IQueue _messageQueue;
        private readonly Stream _stream;
        private bool _disposed;
        private readonly object _writeLock = new object();
        private StreamDictionary _streams;
        private readonly ICompressionProcessor _proc;
        public bool IsPriorityTurnedOn { get; private set; }
        
        public WriteQueue(Stream stream, ICompressionProcessor processor, bool isPriorityTurnedOn)
        {
            if (stream == null)
                throw new ArgumentNullException("io stream is null");

            //if (streams == null)
            //    throw new ArgumentNullException("streams collection is null");

            //Priorities are turned on for debugging
            IsPriorityTurnedOn = isPriorityTurnedOn;
            //_streams = streams;
            _proc = processor;
            if (IsPriorityTurnedOn)
            {
                _messageQueue = new PriorityQueue();
            }
            else
            {
                _messageQueue = new QueueWrapper();
            }
            _stream = stream;
            _disposed = false;
        }

        public void SetStreamDictionary(StreamDictionary streams)
        {
            _streams = streams;
        }

        // Queue up a fully rendered frame to send
        public void WriteFrame(Frame frame)
        {
            if (frame == null)
                throw new ArgumentNullException("frame is null");

            var priority = frame.StreamId != 0 ? _streams[frame.StreamId].Priority : Constants.DefaultStreamPriority;

            IQueueItem entry;

            if (IsPriorityTurnedOn)
            {
                entry = new PriorityQueueEntry(frame, priority);
            }
            else
            {
                entry = new QueueEntry(frame);
            }

            _messageQueue.Enqueue(entry);
        }

        public void PumpToStream(CancellationToken cancel)
        {
            if (cancel == null)
                throw new ArgumentNullException("cancellation token is null");

            while (!_disposed)
            {
                if (cancel.IsCancellationRequested)
                    cancel.ThrowIfCancellationRequested();

                // send one at a time
                lock (_writeLock)
                {
                    if (_messageQueue.Count > 0)
                    {
                        var entry = _messageQueue.Dequeue();

                        /* see https://github.com/MSOpenTech/http2-katana/issues/55
                        It's critically important to keep compression context before sending
                        headers frame. Since that we are unable to construct headers frame with 
                        compressed headers block as part of the frame's Buffer, because Queue has 
                        prioritization mechanism and we must compress headers list immediately before
                        sending it. */
                        if (IsPriorityTurnedOn && entry.Frame is IHeadersFrame && entry.Frame is IPaddingFrame)
                        {
                            /* There are two frame types bears Headers Block Fragment: HEADERS and PUSH_PROMISE
                            and CONTINUATION, which implements IHeadersFrame interface. It can include additional 
                            padding as well. Since that we call to interface methods to avoid code redundant. */

                            // frame reconstruction: headers compression
                            var headers = (entry.Frame as IHeadersFrame).Headers;
                            var compressedHeaders = _proc.Compress(headers);
                            entry.Frame.PayloadLength += compressedHeaders.Length;
                            // frame reconstruction: add padding
                            var paddingFrame = entry.Frame as IPaddingFrame;
                            byte[] padding = new byte[paddingFrame.PadHigh * 256 + paddingFrame.PadLow];
                            entry.Frame.PayloadLength += padding.Length;

                            if (entry.Frame is HeadersFrame)
                            {
                                var headersFrame = entry.Frame as HeadersFrame;
                                Http2Logger.LogDebug("Sending HEADERS frame: stream id={0}, payload len={1}, " +
                                                "has pad={2}, pad high={3}, pad low={4}, end stream={5}, " +
                                                "has priority={6}, exclusive={7}, dependency={8}, weight={9}",
                                headersFrame.StreamId, headersFrame.PayloadLength, headersFrame.HasPadding,
                                headersFrame.PadHigh, headersFrame.PadLow, headersFrame.IsEndStream,
                                headersFrame.HasPriority, headersFrame.Exclusive, headersFrame.StreamDependency,
                                headersFrame.Weight);
                            }
                            if (entry.Frame is PushPromiseFrame)
                            {
                                var pushPromiseFrame = entry.Frame as PushPromiseFrame;
                                Http2Logger.LogDebug("Sending PUSH_PROMISE frame: stream id={0}, payload len={1}, " +
                                                     "promised id={2}, has pad={3}, pad high={4}, pad low={5}, end headers={6}",
                                pushPromiseFrame.StreamId, pushPromiseFrame.PayloadLength,
                                pushPromiseFrame.PromisedStreamId, pushPromiseFrame.HasPadding,
                                pushPromiseFrame.PadHigh, pushPromiseFrame.PadLow, pushPromiseFrame.IsEndHeaders);                 
                            }
                            if (entry.Frame is ContinuationFrame)
                            {
                                var contFrame = entry.Frame as ContinuationFrame;
                                Http2Logger.LogDebug("Sending CONTINUATION frame: stream id={0}, payload len={1}, has pad={2}, " +
                                                     "pad high={3}, pad low={4}, end headers={5}", contFrame.StreamId,
                                contFrame.PayloadLength, contFrame.HasPadding, contFrame.PadHigh, contFrame.PadLow, 
                                contFrame.IsEndHeaders);
                            }

                            // write frame preamble
                            _stream.Write(entry.Buffer, 0, entry.Buffer.Length);
                            // write compressed Headers Block
                            _stream.Write(compressedHeaders, 0, compressedHeaders.Length);
                            // write frame padding
                            _stream.Write(padding, 0, padding.Length);
                        }
                        else
                        {
                            _stream.Write(entry.Buffer, 0, entry.Buffer.Length);
                        }
                    }
                    else
                    {
                        Thread.Sleep(10);
                    }
                }

                _stream.Flush();
            }
        }

        public void Flush()
        {
            while (_messageQueue.Count > 0 && !_disposed)
            {
                var entry = _messageQueue.Dequeue();
                if (entry != null)
                {
                    try
                    {
                        _stream.Write(entry.Buffer, 0, entry.Buffer.Length);
                    }
                    catch (IOException)
                    {
                        Dispose();
                    }
                }
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
        }
    }
}
