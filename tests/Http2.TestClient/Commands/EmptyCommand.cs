namespace Http2.TestClient.Commands
{
    internal sealed class EmptyCommand : Command
    {
        internal override void Parse(string[] cmdArgs)
        {
        }

        internal override CommandType GetCmdType()
        {
            return CommandType.Empty;
        }
    }
}
