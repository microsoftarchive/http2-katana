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
    public static class Http2Logger
    {
        private static readonly string AssemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase.Substring(8));
        private const string FileName = "log.txt";

        private static readonly string FilePath = Path.Combine(AssemblyPath, FileName);
        private static readonly object Locker = new object();

        private static Http2LoggerLevel _level = Http2LoggerLevel.Debug;
        private static bool _writeToFile = true;

        private const string DatePattern = "MM/dd/yy hh:mm:ss.fff";

        #region Properties

        public static Http2LoggerLevel Level
        {
            get { return _level; }
            set { _level = value; }
        }

        public static bool WriteToFile
        {
            get { return _writeToFile; }
            set { _writeToFile = value; }
        }

        #endregion

        public static void Error(string msg, params object[] format)
        {
            if (_level > Http2LoggerLevel.Silly)
            {
                string outString = Format(msg, "ERROR", format);
                Console.WriteLine(outString);
                ToFile(outString);
            }
        }

        public static void Warn(string msg, params object[] format)
        {
            if (_level >= Http2LoggerLevel.Warn)
            {
                string outString = Format(msg, "WARN", format);
                Console.WriteLine(outString);
                ToFile(outString);
            }
        }

        public static void Info(string msg, params object[] format)
        {
            if (_level >= Http2LoggerLevel.Info)
            {
                string outString = Format(msg, "INFO", format);
                Console.WriteLine(outString);
                ToFile(outString);
            }
        }

        public static void Debug(string msg, params object[] format)
        {
            if (_level >= Http2LoggerLevel.Debug)
            {
                string outString = Format(msg, "DEBUG", format);
                Console.WriteLine(outString);
                ToFile(outString);
            }
        }

        public static void StreamStateTransition(int streamId, StreamState previous,
            StreamState current)
        {
            string prev = GetStateName(previous);
            string curr = GetStateName(current);

            if (prev == curr)
                return;

            Debug("State transition: stream id={0} from '{1}' to '{2}'", streamId,
                        prev, curr);
        }

        #region Framing

        public static void FrameSend(Frame frame)
        {
            Frame(frame, "Sending");
        }

        public static void FrameReceived(Frame frame)
        {
            Frame(frame, "Incoming");
        }

        private static void Frame(Frame frame, string action = null)
        {
            switch (frame.FrameType)
            {
                case FrameType.Settings:
                    SettingsFrame(frame as SettingsFrame, action);
                    break;
                case FrameType.Headers:
                    HeadersFrame(frame as HeadersFrame, action);
                    break;
                case FrameType.Data:
                    DataFrame(frame as DataFrame, action);
                    break;
                case FrameType.Continuation:
                    ContinuationFrame(frame as ContinuationFrame, action);
                    break;
                case FrameType.WindowUpdate:
                    WindowUpdateFrame(frame as WindowUpdateFrame, action);
                    break;
                case FrameType.Ping:
                    PingFrame(frame as PingFrame, action);
                    break;
                case FrameType.Priority:
                    PriorityFrame(frame as PriorityFrame, action);
                    break;
                case FrameType.PushPromise:
                    PushPromiseFrame(frame as PushPromiseFrame, action);
                    break;
                case FrameType.RstStream:
                    RstFrame(frame as RstStreamFrame, action);
                    break;
                case FrameType.GoAway:
                    GoAwayFrame(frame as GoAwayFrame, action);
                    break;
            }
        }

        private static void SettingsFrame(SettingsFrame frame, string action = null)
        {
            Debug("{0} SETTINGS frame: stream id={1}, payload len={2}, is ack={3}, count={4}",
                action, frame.StreamId, frame.PayloadLength, frame.IsAck, frame.EntryCount);

            for (int i = 0; i < frame.EntryCount; i++)
            {
                Debug("{0}={1}", frame[i].Id.ToString(), frame[i].Value);
            }
        }

        private static void HeadersFrame(HeadersFrame frame, string action = null)
        {
            Debug("{0} HEADERS frame: stream id={1}, has pad={2}, " +
                     "pad len={3}, end stream={4}, end headers={5}, has priority={6}, " +
                     "exclusive={7}, dependency={8}, weight={9}, count={10}", action,
                     frame.StreamId, frame.HasPadding,
                     frame.PadLength, frame.IsEndStream, frame.IsEndHeaders,
                     frame.HasPriority, frame.Exclusive, frame.StreamDependency,
                     frame.Weight, frame.Headers.Count);

            foreach (var h in frame.Headers)
            {
                Debug("{0}={1}", h.Key, h.Value);
            }
        }

        private static void DataFrame(DataFrame frame, string action = null)
        {
            Debug("{0} DATA frame: stream id={1}, payload len={2}, has pad={3}, pad len={4}, " +
                     "end stream={5}", action, frame.StreamId, frame.PayloadLength,
                     frame.HasPadding, frame.PadLength, frame.IsEndStream);
        }

        private static void WindowUpdateFrame(WindowUpdateFrame frame, string action = null)
        {
            Debug("{0} WINDOW_UPDATE frame: stream id={1}, delta={2}", action, frame.StreamId,
                 frame.Delta);
        }

        private static void PushPromiseFrame(PushPromiseFrame frame, string action = null)
        {
            Debug("{0} PUSH_PROMISE frame: stream id={1}, promised id={2}, " +
                     "has pad={3}, pad len={4}, end headers={5}, count={6}", action,
                     frame.StreamId, frame.PromisedStreamId, frame.HasPadding,
                     frame.PadLength, frame.IsEndHeaders, frame.Headers.Count);

            foreach (var h in frame.Headers)
            {
                Debug("{0}={1}", h.Key, h.Value);
            }
        }

        private static void RstFrame(RstStreamFrame frame, string action = null)
        {
            Warn("{0} RST_STREAM frame: stream id={1}, status code={2}", action,
                     frame.StreamId, frame.StatusCode);
        }

        private static void GoAwayFrame(GoAwayFrame frame, string action = null)
        {
            Debug("{0} GOAWAY frame: stream id={1}, status code={2}", action, 
                frame.StreamId, frame.StatusCode);
        }

        private static void PingFrame(PingFrame frame, string action = null)
        {
            Info("{0} PING frame: stream id={1}, payload={2}", action, frame.StreamId,
                frame.Payload.Count);
        }

        private static void PriorityFrame(PriorityFrame frame, string action = null)
        {
            Debug("{0} PRIORITY frame: stream id={1}, exclusive={2}, dependency={3}, weight={4}",
                     action, frame.StreamId, frame.Exclusive, frame.StreamDependency, frame.Weight);
        }

        private static void ContinuationFrame(ContinuationFrame frame, string action = null)
        {
            Debug("{0} CONTINUATION frame: stream id={1}, payload len={2}, " +
                     " end headers={3}", action, frame.StreamId, frame.PayloadLength,
                     frame.IsEndHeaders);
        }

        #endregion

        private static void ToFile(string message)
        {
            if (!_writeToFile) return;


            lock (Locker)
            {
                using (var file = new StreamWriter(FilePath, true))
                {
                    file.WriteLine(message);
                }
            }
        }

        private static string GetStateName(StreamState state)
        {
            try
            {
                var type = state.GetType();
                return ((Description)(type.GetMember(state.ToString())[0]).GetCustomAttributes(typeof(Description), false)[0]).Name;
            }
            catch(Exception ex)
            {
                return state.ToString().ToLower();
            }            
        }

        private static string Format(string msg, string level, params object[] format)
        {
            msg = string.Format(msg, format);
            return string.Format("[{0}] Thread:{1,-2} {2,-5}: {3}",
                DateTime.Now.ToString(DatePattern), Thread.CurrentThread.ManagedThreadId, level, msg);
        }
    }
}
