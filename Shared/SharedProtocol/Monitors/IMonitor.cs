using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedProtocol
{
    public interface IMonitor
    {
        void Attach(object obj);
        void Detach();
    }
}
