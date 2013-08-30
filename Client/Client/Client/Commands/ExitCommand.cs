namespace Client.Commands
{
    internal sealed class ExitCommand : Command
    {
        internal override CommandType GetCmdType()
        {
            return CommandType.Exit;
        }

        internal override void Parse(string[] cmd)
        {
            
        }
    }
}
