// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
using System;
using System.Collections.Generic;

namespace Http2.TestClient.CommandParser
{
    internal static class HelpDisplayer
    {
        public static Dictionary<int, string> FrequentCommands = new Dictionary<int, string>
                {
                    {1, "get https://localhost:8443/root/simpletest.txt"},
                    {2, "get https://localhost:8443/root/index.html"},
                    {3, "get https://localhost:8443/root/5mbTest.txt"},
                    {4, "get http://localhost:8080/root/simpletest.txt"},
                    {5, "get http://localhost:8080/root/index.html"},
                    {6, "get http://localhost:8080/root/5mbTest.txt"}
                };

        internal static void ShowMainMenu()
        {
            Console.WriteLine("http2-katana client started\n");
            Console.WriteLine("Enter HELP to list available commands\n");
            Console.WriteLine("Enter command number to request one of the following url:\n");
            ShowFrequentCommands();
            Console.WriteLine();
        }

        internal static void ShowHelp()
        {
            //ShowDeleteCommandHelp();
            //ShowDirCommandHelp();
            ShowExitCommandHelp();
            ShowGetCommandHelp();
            ShowHelpCommandHelp();
            ShowPingCommandHelp();
            //ShowPostCommandHelp();
            //ShowPutCommandHelp();
        }

        internal static void ShowExitCommandHelp()
        {
            Console.WriteLine("EXIT   Exit application\n");
            Console.WriteLine("  EXIT does not close current session.");
            Console.WriteLine("\n");
        }

        internal static void ShowHelpCommandHelp()
        {
            Console.WriteLine("HELP   Displays help\n");
            Console.WriteLine("  HELP without arguments displays list of command with short description.");
            Console.WriteLine("  HELP COMMAND displays detailed help for COMMAND.");
            Console.WriteLine("\n");
        }

        internal static void ShowGetCommandHelp()
        {
            Console.WriteLine("GET <url>\n");
            Console.WriteLine("  Download files in working directory.");
            Console.WriteLine("  Sample:\n");
            Console.WriteLine("  GET https://localhost:8443/root/simpletest.txt");
            Console.WriteLine("\n");
        }

        internal static void ShowPutCommandHelp()
        {
            Console.WriteLine("PUT <url> <local url>\n");
            Console.WriteLine("  Upload local file to server.\n");
            Console.WriteLine("  <local url> should be local path to resource.");
            Console.WriteLine("  Upload is done using HTTP/2 protocol.");
            Console.WriteLine("  Examples of PUT:\n");
            Console.WriteLine("  PUT https://localhost:8443/test.html  C:\\test.txt");
            Console.WriteLine("\n");
        }

        internal static void ShowPostCommandHelp()
        {
            Console.WriteLine("POST <server url>/<server action> <local url>\n");
            Console.WriteLine("  Upload local file to server and\n");
            Console.WriteLine("  let server to perform specified action.\n");
            Console.WriteLine("  <local url> should be local path to resource.");
            Console.WriteLine("  <server action> is file name which will be used as handler for incoming data");
            Console.WriteLine("  This attribute is used only for saving file for now");
            Console.WriteLine("  Upload is done using HTTP/2 protocol.");
            Console.WriteLine("  Examples of POST:\n");
            Console.WriteLine("  POST https://localhost:8443/test.html C:\\test.txt view");
            Console.WriteLine("\n");
        }

        internal static void ShowDirCommandHelp()
        {
            Console.WriteLine("DIR <server url>\n");
            Console.WriteLine("  Get files located in a server's root\n");
            Console.WriteLine("  and save result to the index.html\n");
            Console.WriteLine("  located in the client's directory");
            Console.WriteLine("  Examples of Dir:\n");
            Console.WriteLine("  dir https://localhost:8443");
            Console.WriteLine("\n");
        }

        internal static void ShowDeleteCommandHelp()
        {
            Console.WriteLine("DELETE <server url>/<filename>\n");
            Console.WriteLine("  Send delete request to the server\n");
            Console.WriteLine("  This command will always return AccessDenied webpage\n");
            Console.WriteLine("  Examples of Delete:\n");
            Console.WriteLine("  delete https://localhost:8443/index.html");
            Console.WriteLine("\n");
        }

        internal static void ShowPingCommandHelp()
        {
            Console.WriteLine("PING <server url>\n");
            Console.WriteLine(" Pings the remote endpoint if you are connected to it");
            Console.WriteLine(" ping https://localhost:8443/");
            Console.WriteLine("\n");
        }

        internal static void ShowFrequentCommands()
        {
            foreach (var command in FrequentCommands)
            {
                Console.WriteLine("{0} -> {1}", command.Key, command.Value);
            }
        }
    }
}
