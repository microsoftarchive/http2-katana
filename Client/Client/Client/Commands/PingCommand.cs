using System;

namespace Client.Commands
{
    internal sealed class PingCommand : Command
    {
        public Uri Uri { get; private set; }

        internal PingCommand(string[] cmdArgs)
        {
            Parse(cmdArgs);
        }

        internal override CommandType GetCmdType()
        {
            return CommandType.Ping;
        }
        protected override void Parse(string[] cmdArgs)
        {
            Uri uri;
            if (!Uri.TryCreate(cmdArgs[0], UriKind.Absolute, out uri))
            {
                throw new InvalidOperationException("Invalid ping format!");
            }
            Uri = uri;
        }
    }
}
