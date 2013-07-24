using System;

namespace Client.Commands
{
    internal sealed class HelpCommand : Command
    {
        public Action ShowHelp { get; private set; }

        internal HelpCommand(string[] cmdArgs)
        {
            Parse(cmdArgs);
        }

        internal override CommandType GetCmdType()
        {
            return CommandType.Help;
        }

        protected override void Parse(string[] cmdArgs)
        {
            if (cmdArgs.Length == 0)
            {
                ShowHelp = HelpDisplayer.ShowMainMenuHelp;
                return;
            }

            switch (cmdArgs[0].ToLower())
            {
                case "delete":
                    ShowHelp = HelpDisplayer.ShowDeleteCommandHelp;
                    break;
                case "dir":
                    ShowHelp = HelpDisplayer.ShowDirCommandHelp;
                    break;
                case "put":
                    ShowHelp = HelpDisplayer.ShowPutCommandHelp;
                    break;
                case "post":
                    ShowHelp = HelpDisplayer.ShowPostCommandHelp;
                    break;
                case "get":
                    ShowHelp = HelpDisplayer.ShowGetCommandHelp;
                    break;
                case "help":
                    ShowHelp = HelpDisplayer.ShowHelpCommandHelp;
                    break;
                case "exit":
                    ShowHelp = HelpDisplayer.ShowExitCommandHelp;
                    break;
                case "ping":
                    ShowHelp = HelpDisplayer.ShowPingCommandHelp;
                    break;
                default:
                    Console.WriteLine("Help was called for non-implemented command");
                    break;
            }
        }
    }
}
