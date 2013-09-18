namespace Http2.TestClient.Commands
{
    internal abstract class Command
    {
        abstract internal CommandType GetCmdType();
        abstract internal void Parse(string[] cmdArgs);
    }
}
