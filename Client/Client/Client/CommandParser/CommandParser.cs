using System;

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

            var joinedCmd = new string[splittedCmd.Length - 1];
            Array.Copy(splittedCmd, 1, joinedCmd, 0, joinedCmd.Length);

            var cmdBody = String.Concat(joinedCmd).Trim();

            switch (splittedCmd[0].ToLower())
            {
                case "get":
                    return new GetCommand(cmdBody);
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
                    return new HelpCommand(cmdBody);
                case "exit":
                    return new ExitCommand();
            }
            return new UnknownCommand(splittedCmd[0]);
        }
    }
}
