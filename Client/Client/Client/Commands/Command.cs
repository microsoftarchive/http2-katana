namespace Client.Commands
{
    internal abstract class Command
    {
        abstract internal CommandType GetCmdType();
        abstract protected void Parse(string[] cmdArgs);
    }
}
