using System;

namespace Microsoft.Http2.Push.Exceptions
{
    public class ReferenceTableHasCycleException : Exception
    {
        public ReferenceTableHasCycleException()
            : base("Reference table has cycle!")
        {
            
        }
    }
}
