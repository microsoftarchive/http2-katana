// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.

// -----------------------------------------------------------------------
// <copyright file="Http2Logger.cs" company="Microsoft Open Technologies, Inc.">
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
// </copyright>
// -----------------------------------------------------------------------
using System.IO;
using System;
using System.Reflection;
using System.Threading;

namespace Microsoft.Http2.Protocol.Utils
{
    /// <summary>
    /// Http2 logger class
    /// </summary>   
    public static class Http2Logger
    {
        private static readonly string AssemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase.Substring(8));
        private const string LogFileName = "log.txt";

        private static readonly string LogFilePath = Path.Combine(AssemblyPath, LogFileName);
        private static readonly object Locker = new object();

        private static Http2LoggerState _loggerLevel = Http2LoggerState.MaxLogging;
        private static bool _writeToFile = true;

        #region Properties

        /// <summary>
        /// Gets or sets logging level.
        /// </summary>
        public static Http2LoggerState LoggerLevel
        {
            get { return _loggerLevel; }
            set { _loggerLevel = value; }
        }

        /// <summary>
        /// Gets or sets whenever message will be logged to a file.
        /// </summary>
        public static bool WriteToFile
        {
            get { return _writeToFile; }
            set { _writeToFile = value; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Logs error.
        /// </summary>
        /// <param name="errString">String to log</param>
        public static void LogError(string errString)
        {
            if (_loggerLevel > Http2LoggerState.NoLogging)
            {
                string outString = string.Format("[{0}] ThreadId:{1} ERROR: {2}", DateTime.Now, Thread.CurrentThread.ManagedThreadId, errString);
                Console.WriteLine(outString);
                LogToFile(outString);
            }
        }

        /// <summary>
        /// Console output messages for interactive users.
        /// </summary>
        /// <param name="consoleString">String to display</param>
        public static void LogConsole(string consoleString)
        {
            if ((_loggerLevel > Http2LoggerState.NoLogging))
            {
                string outString = "[" + DateTime.Now + "] " + consoleString;
                Console.WriteLine(outString);
            }
        }

        /// <summary>
        /// Logs informational message.
        /// </summary>
        /// <param name="infoString">String to log</param>
        public static void LogInfo(string infoString)
        {
            if (_loggerLevel >= Http2LoggerState.VerboseLogging)
            {
                string outString = "[" + DateTime.Now.ToString("T") + "] INFO: " + infoString;
                Console.WriteLine(outString);
                LogToFile(outString);
            }
        }

        /// <summary>
        /// Logs debug message.
        /// </summary>
        /// <param name="debugString">String to log</param>
        /// <param name="format">String format.</param>
        public static void LogDebug(string debugString, params object[] format)
        {
            if (_loggerLevel >= Http2LoggerState.DebugLogging)
            {
                debugString = string.Format(debugString, format);
                string outString = string.Format("[{0}] ThreadId:{1} DBG: {2}", DateTime.Now, Thread.CurrentThread.ManagedThreadId, debugString);
                Console.WriteLine(outString);
                LogToFile(outString);
            }
        }

        public static void LogHeaders(HeadersList headers)
        {
            Console.WriteLine("Headers set:");
            foreach (var header in headers)
            {
                Console.WriteLine("{0}: {1}", header.Key, header.Value);
            }
        }

        private static void LogToFile(string message)
        {
            if (!_writeToFile) return;


            lock (Locker)
            {
                using (var logFile = new StreamWriter(LogFilePath, true))
                {
                    logFile.WriteLine(message);
                }
            }
        }

        #endregion
    }
}
