using System;
using Client.Commands;

namespace Client.CommandParser
{
    internal static class CommandParser
    {
        internal static Command Parse(string input)
        {
            var splittedCmd = input.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);

            if (splittedCmd.Length == 0)
            {
                return new EmptyCommand();
            }

            var cmdArgs = new string[splittedCmd.Length - 1];
            Array.Copy(splittedCmd, 1, cmdArgs, 0, cmdArgs.Length);

            Command cmd = null;
            switch (splittedCmd[0].ToLower())
            {
                case "dir":
                    cmd = new DirCommand();
                    break;
                case "post":
                    cmd = new PostCommand();
                    break;
                case "put":
                    cmd = new PutCommand();
                    break;
                case "get":
                    cmd = new GetCommand();
                    break;
                case "delete":
                    cmd = new DeleteCommand();
                    break;
                case "connect":
                    break;
                case "disconnect":
                    break;
                case "capturestatson":
                    break;
                case "capturestatsoff":
                    break;
                case "help":
                    cmd = new HelpCommand();
                    break;
                case "exit":
                    cmd = new ExitCommand();
                    break;
                case "ping":
                    cmd = new PingCommand();
                    break;
                default:
                    cmd = new UnknownCommand(splittedCmd[0]);
                    break;
            }
            cmd.Parse(cmdArgs);
            return cmd;
        }
    }
}
