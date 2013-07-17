namespace Client.Commands
{
    internal sealed class EmptyCommand : Command
    {
        protected override void Parse(string[] cmdArgs)
        {
        }

        internal override CommandType GetCmdType()
        {
            return CommandType.Empty;
        }
    }
}
