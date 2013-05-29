using System;

namespace Client
{
    internal sealed class GetCommand : Command
    {
        private Uri _uri;

        public Uri Uri
        {
            get { return _uri; }
        }
        
        internal GetCommand(string cmdBody)
        {
            Parse(cmdBody);
        }

        protected override void Parse(string cmd)
        {
            if (Uri.TryCreate(cmd, UriKind.Absolute, out _uri) == false)
            {
                throw new InvalidOperationException("Invalid Get command!");
            }
        }

        internal override CommandType GetCmdType()
        {
            return CommandType.Get;
        }
    }
}
