using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    internal class PingCommand : Command
    {
        public Uri Uri { get; private set; }

        internal PingCommand(string cmd)
        {
            Parse(cmd);
        }

        internal override CommandType GetCmdType()
        {
            return CommandType.Ping;
        }
        protected override void Parse(string cmd)
        {
            Uri uri;
            if (!Uri.TryCreate(cmd, UriKind.Absolute, out uri))
            {
                throw new InvalidOperationException("Invalid ping command!");
            }
            Uri = uri;
        }
    }
}
