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

namespace Client
{
    /// <summary>
    /// Main client class
    /// </summary>   
    public class Program
    {
        private static Dictionary<string, Http2SessionHandler> _sessions;
        private static bool useHandshake = true;
        private static bool usePrioritization = false;
        private static bool useFlowControl = true;

        public static void Main(string[] args)
        {
            _sessions = new Dictionary<string, Http2SessionHandler>();
            var argsList = new List<string>(args);

            useHandshake = !argsList.Contains("-no-handshake");
            usePrioritization = argsList.Contains("use-priorities");
            useFlowControl = argsList.Contains("-no-flowcontrol");
            HelpDisplayer.ShowMainMenuHelp();
            ThreadPool.SetMaxThreads(10, 10);
            try
            {
                Console.WriteLine("Enter command");
                while (true)
                {
                    Console.Write(">");
                    string command = Console.ReadLine();
                    Command cmd = null;
                    
                    try
                    {
                        cmd = CommandParser.Parse(command);
                    }
                    catch (InvalidOperationException ex)
                    {
                        Console.WriteLine(ex.Message);
                        continue;
                    }
                    //Scheme and port were checked during parsing get cmd.
                    switch (cmd.GetCmdType())
                    {
                        case CommandType.Get:
                            var getCmd = (GetCommand) cmd;

                            //Only unique sessions can be opened
                            if (_sessions.ContainsKey(getCmd.Uri.Authority))
                            {
                                _sessions[getCmd.Uri.Authority].SendRequestAsync(getCmd.Uri);
                                break;
                            }

                            var sessionHandler = new Http2SessionHandler(getCmd.Uri, useHandshake, usePrioritization, useFlowControl);

                            //Get cmd is equivalent for connect -> get. This means, that each get request 
                            //will open new session.
                            ThreadPool.QueueUserWorkItem(delegate
                            {
                                sessionHandler.Connect();
                                sessionHandler.SendRequestAsync(getCmd.Uri);
                            });

                            _sessions.Add(getCmd.Uri.Authority, sessionHandler);
                            break;
                        case CommandType.Help:
                            ((HelpCommand)cmd).ShowHelp.Invoke();
                            break;
                        case CommandType.Ping:
                            _sessions[((PingCommand)cmd).Uri.Authority].Ping();
                            break;
                        case CommandType.Exit:
                            foreach (var sessionUri in _sessions.Keys)
                            {
                                _sessions[sessionUri].Dispose();
                                _sessions.Remove(sessionUri);
                            }
                            return;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }                 
        }
    }
}
