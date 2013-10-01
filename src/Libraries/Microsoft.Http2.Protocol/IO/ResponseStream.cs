using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Http2.Protocol.EventArgs;

namespace Microsoft.Http2.Protocol.IO
{
    public class ResponseStream : MemoryStream
    {
        public override void Write(byte[] buffer, int offset, int count)
        {
            base.Write(buffer, offset, count);

            if (OnDataWritten != null)
                OnDataWritten(this, new DataIsWrittenEventArgs(count));
        }

        /*public override void WriteTo(Stream stream)
        {
            base.WriteTo(stream);
        }*/

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await base.WriteAsync(buffer, offset, count, cancellationToken);

            if (OnDataWritten != null)
                OnDataWritten(this, new DataIsWrittenEventArgs(count));
        }

        public override void WriteByte(byte value)
        {
            base.WriteByte(value);

            if (OnDataWritten != null)
                OnDataWritten(this, new DataIsWrittenEventArgs(1));
        }

        /*public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return base.BeginWrite(buffer, offset, count, callback, state);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            base.EndWrite(asyncResult);

            if (OnDataWritten != null)
                OnDataWritten(this, new DataIsWrittenEventArgs(asyncResult., 0, 1));
        }*/

        public event EventHandler<DataIsWrittenEventArgs> OnDataWritten;
    }
}
