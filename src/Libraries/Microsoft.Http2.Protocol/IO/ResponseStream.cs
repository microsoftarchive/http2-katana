using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Http2.Protocol.EventArgs;

namespace Microsoft.Http2.Protocol.IO
{
    public class ResponseStream : MemoryStream
    {
        public event EventHandler<DataIsWrittenEventArgs> OnDataWritten;

        public override void Write(byte[] buffer, int offset, int count)
        {
            base.Write(buffer, offset, count);

            if (OnDataWritten != null)
                OnDataWritten(this, new DataIsWrittenEventArgs(count));
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return base.WriteAsync(buffer, offset, count, cancellationToken).ContinueWith(task =>
                {
                    if (OnDataWritten != null)
                        OnDataWritten(this, new DataIsWrittenEventArgs(count));
                });
        }

        public override void WriteByte(byte value)
        {
            base.WriteByte(value);

            if (OnDataWritten != null)
                OnDataWritten(this, new DataIsWrittenEventArgs(1));
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
    }
}
