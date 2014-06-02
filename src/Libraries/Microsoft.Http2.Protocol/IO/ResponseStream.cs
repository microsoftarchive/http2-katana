// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Http2.Protocol.Extensions;

namespace Microsoft.Http2.Protocol.IO
{
    public class ResponseStream : Stream
    {
        private Http2Stream _stream;
        private Action _onFirstWrite;
        private bool _firstWrite = true;

        public ResponseStream(Http2Stream stream, Action onFirstWrite)
        {
            _stream = stream;
            _onFirstWrite = onFirstWrite;
        }

        public override bool CanRead
        {
            get { return false; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override long Position
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        private void FirstWrite()
        {
            if (_firstWrite)
            {
                _firstWrite = false;
                _onFirstWrite();
            }
        }

        public override void Flush()
        {
            FirstWrite();
            // TODO: Flush all frames to the wire.
            // _stream.Flush();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            FirstWrite();
            // TODO: Validate buffer, offset, count

            int sent = 0;
            while (sent < count)
            {
                // maximum chunk length of the file to be read
                int chunkSize = MathEx.Min(count - sent, Constants.MaxFramePayloadSize - Constants.MaxFramePaddingSize);
                var segment = new ArraySegment<byte>(buffer, offset + sent, chunkSize);
                _stream.WriteDataFrame(segment, isEndStream: false);
                sent += chunkSize;
            }
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Write(buffer, offset, count);
            return Task.FromResult(0);
        }
        
        //We may not override BeginWrite method because
        //http://msdn.microsoft.com/ru-ru/library/system.io.memorystream.aspx
        //See BeginWrite method. It's inherited from Stream.BeginWrite
        //See it's note: 
        //http://msdn.microsoft.com/ru-ru/library/system.io.stream.beginwrite.aspx
        //The default implementation of BeginWrite on a stream calls the Write method synchronously, 
        //which means that Write might block on some streams.
        //It's better to use sync operations because of data mixing.
        //See: 
        //Any public static (Shared in Visual Basic) members of this type are thread safe. 
        //Any instance members are not guaranteed to be thread safe.

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }
    }
}
