// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Http2.TestClient.CommandParser;
using Http2.TestClient.Commands;
using Microsoft.Http2.Protocol.Utils;
using OpenSSL.SSL;

namespace Http2.TestClient
{
    /// <summary>
    /// Main client class
    /// </summary>   
    public class Program
    {
        private static Dictionary<string, Http2SessionHandler> _sessions;
        private static IDictionary<string, object> _environment;

        public static void Main(string[] args)
        {
            Console.SetWindowSize(125, 29);
            Http2Logger.WriteToFile = false;

            _sessions = new Dictionary<string, Http2SessionHandler>();
            var argsList = new List<string>(args);

            _environment = new Dictionary<string, object>
                {
                    {"useHandshake", !argsList.Contains("-no-handshake")},
                    {"usePriorities", !argsList.Contains("-no-priorities")},
                    {"useFlowControl", !argsList.Contains("-no-flowcontrol")},
                };

            HelpDisplayer.ShowMainMenuHelp();
            ThreadPool.SetMaxThreads(10, 10);

            Console.WriteLine("Enter command");
            while (true)
            {
                try
                {
                    Console.Write(">");
                    string command = Console.ReadLine();
                    Command cmd;

                    try
                    {
                        cmd = CommandParser.CommandParser.Parse(command);
                    }
                    catch (Exception ex)
                    {
                        Http2Logger.LogError(ex.Message);
                        continue;
                    }
                    //Scheme and port were checked during parsing get cmd.
                    switch (cmd.GetCmdType())
                    {
                        case CommandType.Put:
                        case CommandType.Post:
                        case CommandType.Get:
                        case CommandType.Delete:
                        case CommandType.Dir:
                            var uriCmd = (IUriCommand)cmd;

                            string method = uriCmd.Method;

                            //Only unique sessions can be opened
                            if (_sessions.ContainsKey(uriCmd.Uri.Authority))
                            {
                                _sessions[uriCmd.Uri.Authority].SendRequestAsync(uriCmd.Uri, method);
                                break;
                            }

                            var sessionHandler = new Http2SessionHandler(_environment);
                            _sessions.Add(uriCmd.Uri.Authority, sessionHandler);
                            sessionHandler.OnClosed +=
                                (sender, eventArgs) => _sessions.Remove(sessionHandler.ServerUri);

                            //Get cmd is equivalent for connect -> get. This means, that each get request 
                            //will open new session.
                            bool success = sessionHandler.Connect(uriCmd.Uri);
                            if (!success)
                            {
                                Http2Logger.LogError("Connection failed");
                                break;
                            }

                            if (!sessionHandler.WasHttp1Used)
                            {
                                Task.Run(() => sessionHandler.StartConnection());

                                using (var waitForConnectionStart = new ManualResetEvent(false))
                                {
                                    waitForConnectionStart.WaitOne(500);
                                }

                                if (sessionHandler.Protocol != SslProtocols.None)
                                    sessionHandler.SendRequestAsync(uriCmd.Uri, method);
                            }
                            break;
                        case CommandType.Help:
                            ((HelpCommand)cmd).ShowHelp.Invoke();
                            break;
                        case CommandType.Ping:
                            string url = ((PingCommand)cmd).Uri.Authority;
                            if (_sessions.ContainsKey(url))
                            {
                                _sessions[url].Ping();
                            }
                            else
                            {
                                Http2Logger.LogError("Can't ping until session is opened.");
                            }
                            break;
                        case CommandType.Exit:

                            var sessionsDictCopy = new Dictionary<string, Http2SessionHandler>(_sessions);
                            foreach (var sessionUri in sessionsDictCopy.Keys)
                            {
                                sessionsDictCopy[sessionUri].Dispose(false);
                            }
                            sessionsDictCopy.Clear();
                            return;
                    }
                }
                catch (Exception e)
                {
                    Http2Logger.LogError("Problems occurred - please restart client. Error: " + e.Message);
                }
            }
        }
    }
}
