using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedProtocol.IO
{
    internal interface IQueueItem
    {
        byte[] Buffer { get; }
    }
}
