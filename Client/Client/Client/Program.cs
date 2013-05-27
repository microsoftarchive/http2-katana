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

namespace Client
{
    /// <summary>
    /// Main client class
    /// </summary>   
    public class Program
    {

        private static Dictionary<string, Http2SessionHandler> hostSession;

        public static void Main(string[] args)
        {
            hostSession = new Dictionary<string, Http2SessionHandler>();
            try
            {
                Console.WriteLine("Type connect cmd for session opening");
                while (true)
                {
                    string command = Console.ReadLine();

                    switch (command)
                    {
                        case "connect":
                            string connectString = "http://localhost:8443/test.txt";
                            Uri uri;
                            Uri.TryCreate(connectString, UriKind.Absolute, out uri);

                            var sessionHandler = new Http2SessionHandler(uri);
                            sessionHandler.Connect();

                            hostSession.Add("http://localhost:8443/", sessionHandler);
                            break;
                        case "get":
                            ThreadPool.QueueUserWorkItem(delegate
                                {
                                    hostSession["http://localhost:8443/"].SendRequestAsync();
                                    hostSession["http://localhost:8443/"].WaitForResponse();
                                });
                            break;
                        case "disconnect":
                            hostSession["http://localhost:8443/"].Dispose();
                            hostSession.Remove("http://localhost:8443/");
                            break;
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
