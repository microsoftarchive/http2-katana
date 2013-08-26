namespace Client.Commands
{
    internal abstract class Command
    {
        abstract internal CommandType GetCmdType();
        abstract internal void Parse(string[] cmdArgs);
    }
}
