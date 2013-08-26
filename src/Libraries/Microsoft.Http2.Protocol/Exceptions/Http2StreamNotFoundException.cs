using System;

namespace SharedProtocol.Exceptions
{
    public class Http2StreamNotFoundException : Exception
    {
        public int Id { get; private set; }

        public Http2StreamNotFoundException(int id)
            : base(String.Format("Stream was not found by provided id: {0}", id))
        {
            Id = id;
        }
    }
}
