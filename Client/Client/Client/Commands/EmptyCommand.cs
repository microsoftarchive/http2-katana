using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    internal class EmptyCommand : Command
    {
        protected override void Parse(string cmd)
        {
        }

        internal override CommandType GetCmdType()
        {
            return CommandType.Empty;
        }
    }
}
