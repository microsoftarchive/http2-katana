using System;

namespace Microsoft.Http2.Protocol.Exceptions
{
    internal class MaxConcurrentStreamsLimitException : Exception
    {
        internal MaxConcurrentStreamsLimitException()
            :base("Endpoint is trying to create more streams then allowed!")
        {
            
        }
    }
}
