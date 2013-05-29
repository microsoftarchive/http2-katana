namespace Client
{
    internal abstract class Command
    {
        abstract internal CommandType GetCmdType();
        abstract protected void Parse(string cmd);
    }
}
