// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
using Http2.TestClient.CommandParser;
using System;

namespace Http2.TestClient.Commands
{
    internal sealed class HelpCommand : Command
    {
        public Action ShowHelp { get; private set; }

        internal HelpCommand()
        {
        }

        internal override CommandType GetCmdType()
        {
            return CommandType.Help;
        }

        internal override void Parse(string[] cmdArgs)
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
