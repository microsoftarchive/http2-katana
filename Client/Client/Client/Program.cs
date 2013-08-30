//-----------------------------------------------------------------------
// <copyright file="Program.cs" company="Microsoft Open Technologies, Inc.">
//
// The copyright in this software is being made available under the BSD License, included below. 
// This software may be subject to other third party and contributor rights, including patent rights, 
// and no such rights are granted under this license.
//
// Copyright (c) 2012, Microsoft Open Technologies, Inc. 
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without modification, 
// are permitted provided that the following conditions are met:
//
// - Redistributions of source code must retain the above copyright notice, 
//   this list of conditions and the following disclaimer.
// - Redistributions in binary form must reproduce the above copyright notice, 
//   this list of conditions and the following disclaimer in the documentation 
//   and/or other materials provided with the distribution.
// - Neither the name of Microsoft Open Technologies, Inc. nor the names of its contributors 
//   may be used to endorse or promote products derived from this software 
//   without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS “AS IS” AND ANY EXPRESS OR IMPLIED WARRANTIES, 
// INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS 
// FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, 
// INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, 
// PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) 
// HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, 
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, 
// EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//
//
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Client.CommandParser;
using Client.Commands;
using Org.Mentalis.Security.Ssl;
using Microsoft.Http2.Protocol.Utils;

namespace Client
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
                            string localPath = null;
                            string serverPostAct = null;

                            if (cmd is PostCommand)
                            {
                                localPath = (cmd as PostCommand).LocalPath;
                                serverPostAct = (cmd as PostCommand).ServerPostAct;
                            }
                            else if (cmd is PutCommand)
                            {
                                localPath = (cmd as PutCommand).LocalPath;
                            }

                            //Only unique sessions can be opened
                            if (_sessions.ContainsKey(uriCmd.Uri.Authority))
                            {
                                _sessions[uriCmd.Uri.Authority].SendRequestAsync(uriCmd.Uri, method, localPath, serverPostAct);
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

                            Task.Run(() => sessionHandler.StartConnection());

                            using (var waitForConnectionStart = new ManualResetEvent(false))
                            {
                                waitForConnectionStart.WaitOne(200);
                            }
                            if (sessionHandler.Options.Protocol != SecureProtocol.None)
                                sessionHandler.SendRequestAsync(uriCmd.Uri, method, localPath, serverPostAct);
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
                            foreach (var sessionUri in _sessions.Keys)
                            {
                                _sessions[sessionUri].Dispose(false);
                            }
                            _sessions.Clear();
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
