namespace Client
{
    internal class ExitCommand : Command
    {
        internal override CommandType GetCmdType()
        {
            return CommandType.Exit;
        }

        protected override void Parse(string cmd)
        {
            
        }
    }
}
