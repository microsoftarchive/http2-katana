using System;

namespace Client
{
    internal class HelpCommand : Command
    {
        public Action ShowHelp { get; private set; }

        internal HelpCommand(string cmdBody)
        {
            Parse(cmdBody);
        }
        internal override CommandType GetCmdType()
        {
            return CommandType.Help;
        }

        protected override void Parse(string cmdBody)
        {
            if (String.IsNullOrEmpty(cmdBody))
            {
                HelpDisplayer.ShowMainMenuHelp();
                return;
            }

            switch (cmdBody.ToLower())
            {
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
