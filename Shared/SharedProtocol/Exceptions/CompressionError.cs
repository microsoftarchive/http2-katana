using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedProtocol.Exceptions
{
    public class CompressionError : Exception
    {
        public CompressionError(Exception e): base("", e)
        {

        }

        public override string Message
        {
            get
            {
                return InnerException != null ? InnerException.Message : base.Message;
            }
        }
    }
}
