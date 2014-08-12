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
using Microsoft.Http2.Protocol.Framing;

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

        public static void LogFrameSend(Frame frame)
        {
            LogFrame(frame, "Sending");
        }

        public static void LogFrameReceived(Frame frame)
        {
            LogFrame(frame, "Incoming");
        }

        public static void LogFrame(Frame frame, string action = null)
        {
            switch (frame.FrameType)
            {
                case FrameType.Settings:
                    LogSettingsFrame(frame as SettingsFrame, action);
                    break;
                case FrameType.Headers:
                    LogHeadersFrame(frame as HeadersFrame, action);
                    break;
                case FrameType.Data:
                    LogDataFrame(frame as DataFrame, action);
                    break;
                case FrameType.Continuation:
                    LogContinuationFrame(frame as ContinuationFrame, action);
                    break;
                case FrameType.WindowUpdate:
                    LogWindowUpdateFrame(frame as WindowUpdateFrame, action);
                    break;
                case FrameType.Ping:
                    LogPingFrame(frame as PingFrame, action);
                    break;
                case FrameType.Priority:
                    LogPriorityFrame(frame as PriorityFrame, action);
                    break;
                case FrameType.PushPromise:
                    LogPushPromiseFrame(frame as PushPromiseFrame, action);
                    break;
                case FrameType.RstStream:
                    LogRstFrame(frame as RstStreamFrame, action);
                    break;
                case FrameType.GoAway:
                    LogGoAwayFrame(frame as GoAwayFrame, action);
                    break;
            }
        }

        private static void LogSettingsFrame(SettingsFrame frame, string action = null)
        {
            LogDebug("{0} SETTINGS frame: stream id={1}, payload len={2}, is ack={3}, count={4}",
                action, frame.StreamId, frame.PayloadLength, frame.IsAck, frame.EntryCount);

            for (int i = 0; i < frame.EntryCount; i++)
            {
                LogDebug("{0}={1}", frame[i].Id.ToString(), frame[i].Value);
            }
        }

        private static void LogHeadersFrame(HeadersFrame frame, string action = null)
        {
            LogDebug("{0} HEADERS frame: stream id={1}, payload len={2}, has pad={3}, " +
                     "pad len={4}, end stream={5}, end headers={6}, has priority={7}, " +
                     "exclusive={8}, dependency={9}, weight={10}, count={11}", action,
                     frame.StreamId, frame.PayloadLength, frame.HasPadding,
                     frame.PadLength, frame.IsEndStream, frame.IsEndHeaders,
                     frame.HasPriority, frame.Exclusive, frame.StreamDependency,
                     frame.Weight, frame.Headers.Count);

            foreach (var h in frame.Headers)
            {
                LogDebug("{0}={1}", h.Key, h.Value);
            }
        }

        private static void LogDataFrame(DataFrame frame, string action = null)
        {
            LogDebug("{0} DATA frame: stream id={1}, payload len={2}, has pad={3}, pad len={4}, " +
                     "end stream={5}", action, frame.StreamId, frame.PayloadLength,
                     frame.HasPadding, frame.PadLength, frame.IsEndStream);
        }

        private static void LogWindowUpdateFrame(WindowUpdateFrame frame, string action = null)
        {
            LogDebug("{0} WINDOW_UPDATE frame: stream id={1}, delta={2}", action, frame.StreamId,
                 frame.Delta);
        }

        private static void LogPushPromiseFrame(PushPromiseFrame frame, string action = null)
        {
            LogDebug("{0} PUSH_PROMISE frame: stream id={1}, payload len={2}, promised id={3}, " +
                     "has pad={4}, pad len={5}, end headers={6}, count={7}", action,
                     frame.StreamId, frame.PayloadLength, frame.PromisedStreamId, frame.HasPadding,
                     frame.PadLength, frame.IsEndHeaders, frame.Headers.Count);

            foreach (var h in frame.Headers)
            {
                LogDebug("{0}={1}", h.Key, h.Value);
            }
        }

        private static void LogRstFrame(RstStreamFrame frame, string action = null)
        {
            LogDebug("{0} RST_STREAM frame: stream id={1}, status code={2}", action,
                     frame.StreamId, frame.StatusCode);
        }

        private static void LogGoAwayFrame(GoAwayFrame frame, string action = null)
        {
            LogDebug("{0} GOAWAY frame: stream id={1}, status code={2}", action, 
                frame.StreamId, frame.StatusCode);
        }

        private static void LogPingFrame(PingFrame frame, string action = null)
        {
            LogDebug("{0} PING frame: stream id={1}, payload={2}", action, frame.StreamId,
                frame.Payload.Count);
        }

        private static void LogPriorityFrame(PriorityFrame frame, string action = null)
        {
            LogDebug("{0} PRIORITY frame: stream id={1}, exclusive={2}, dependency={3}, weight={4}",
                     action, frame.StreamId, frame.Exclusive, frame.StreamDependency, frame.Weight);
        }

        private static void LogContinuationFrame(ContinuationFrame frame, string action = null)
        {
            LogDebug("{0} CONTINUATION frame: stream id={1}, payload len={2}" +
                     " end headers={3}", action, frame.StreamId, frame.PayloadLength,
                     frame.IsEndHeaders);
        }

        public static void LogHeaders(HeadersList headers)
        {
            Console.WriteLine("Headers set:");
            foreach (var header in headers)
            {
                Console.WriteLine("{0}={1}", header.Key, header.Value);
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
