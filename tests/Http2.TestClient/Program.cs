// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Configuration;
using Http2.TestClient.CommandParser;
using Http2.TestClient.Commands;
using Microsoft.Http2.Protocol;
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

        private static Dictionary<string, object> CreateEnvironment(Uri uri)
        {
            var serverName = uri.Host;
            var isDirectEnabled = ClientOptions.IsDirectEnabled;
            var environment = new Dictionary<string, object>
                {
                    {Strings.ServerName, serverName},
                    {Strings.DirectEnabled, isDirectEnabled}
                };

            return environment;
        }

        public static void Main(string[] args)
        {
            Console.SetWindowSize(125, 29);
            Http2Logger.WriteToFile = false;

            _sessions = new Dictionary<string, Http2SessionHandler>();
       
            var isTestsEnabled = ClientOptions.IsTestModeEnabled;
            
            var waitForTestsFinish = new ManualResetEvent(!isTestsEnabled);

            Http2Logger.Info("Tests enabled: " + isTestsEnabled);

            ThreadPool.SetMaxThreads(10, 10);

            var uri = args.FirstOrDefault(a => a.Contains(Strings.Http));
            if (!isTestsEnabled)
            {
                HelpDisplayer.ShowMainMenu();
                Console.WriteLine("Enter command:");
            }
            
            do
            {
                try
                {
                    Command cmd;
                    string command;
                    if (uri != null)
                    {
                        command = Verbs.Get + " " + uri; //TODO set up method correctly
                    }
                    else
                    {
                        Console.Write(">");
                        command = Console.ReadLine();
                    }

                    try
                    {
                        cmd = CommandParser.CommandParser.Parse(command);
                    }
                    catch (Exception ex)
                    {
                        Http2Logger.Error(ex.Message);
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
                            Http2Logger.Info("Uri command detected");
                            var uriCmd = (IUriCommand) cmd;
                            var method = uriCmd.Method;

                            _environment = CreateEnvironment(uriCmd.Uri);
                            //Only unique sessions can be opened
                            if (_sessions.ContainsKey(uriCmd.Uri.Authority))
                            {
                                Http2Logger.Warn("Session already exists");
                                _sessions[uriCmd.Uri.Authority].SendRequestAsync(uriCmd.Uri, method);
                                break;
                            }

                            Http2Logger.Info("Creating new session");
                            var sessionHandler = new Http2SessionHandler(_environment);
                            _sessions.Add(uriCmd.Uri.Authority, sessionHandler);
                            sessionHandler.OnClosed +=
                                (sender, eventArgs) =>
                                    {
                                        _sessions.Remove(sessionHandler.ServerUri);
                                        Http2Logger.Info("Session deleted from collection: " + sessionHandler.ServerUri);

                                        waitForTestsFinish.Set();
                                    };

                            //Get cmd is equivalent for connect -> get. This means, that each get request 
                            //will open new session.
                            Console.WriteLine(uriCmd.Uri.ToString());
                            bool success = sessionHandler.Connect(uriCmd.Uri);
                            if (!success)
                            {
                                Http2Logger.Error("Connection failed");
                                break;
                            }

                            if (!sessionHandler.WasHttp1Used)
                            {
                                sessionHandler.StartConnection();

                                if (sessionHandler.Protocol != SslProtocols.None)
                                {
                                    sessionHandler.SendRequestAsync(uriCmd.Uri, method);
                                }
                            }
                            break;
                        case CommandType.Help:
                            if (((HelpCommand)cmd).ShowHelp != null)
                                ((HelpCommand) cmd).ShowHelp.Invoke();
                            break;
                        case CommandType.Ping:
                            string url = ((PingCommand) cmd).Uri.Authority;
                            if (_sessions.ContainsKey(url))
                            {
                                _sessions[url].Ping();
                            }
                            else
                            {
                                Http2Logger.Info("Can't ping until session is opened.");
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
                    Http2Logger.Error("Problems occurred - please restart client. Error: " + e.Message);
                }
            } while (!isTestsEnabled);

            waitForTestsFinish.WaitOne(5000);

            Http2Logger.Info("Exiting");
            Console.WriteLine();
            Console.WriteLine();
        }
    }
}
