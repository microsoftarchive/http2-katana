using System;
using Client.Commands;

namespace Client
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

            switch (splittedCmd[0].ToLower())
            {
                case "post":
                    return new PostCommand(cmdArgs);
                case "put":
                    return new PutCommand(cmdArgs);
                case "get":
                    return new GetCommand(cmdArgs);
                case "delete":
                    return new DeleteCommand(cmdArgs);
                case "connect":
                    break;
                case "disconnect":
                    break;
                case "capturestatson":
                    break;
                case "capturestatsoff":
                    break;
                case "dir":
                    break;
                case "help":
                    return new HelpCommand(cmdArgs);
                case "exit":
                    return new ExitCommand();
                case "ping":
                    return new PingCommand(cmdArgs);
            }
            return new UnknownCommand(splittedCmd[0]);
        }
    }
}
