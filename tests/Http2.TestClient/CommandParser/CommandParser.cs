// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
using System;
using Http2.TestClient.Commands;

namespace Http2.TestClient.CommandParser
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
