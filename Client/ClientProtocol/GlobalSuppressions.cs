// <copyright file="GlobalSuppressions.cs" company="Microsoft Open Technologies, Inc.">
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

// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.
//
// To add a suppression to this file, right-click the message in the 
// Error List, point to "Suppress Message(s)", and click 
// "In Project Suppression File".
// You do not need to add suppressions to this file manually.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA2210:AssembliesShouldHaveValidStrongNames", Justification = "Strong name signing not required for codeplex.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "wss", Scope = "member", Target = "System.ServiceModel.WebSockets.WebSocketProtocol.#.ctor(System.String,System.String,System.String,System.Boolean)", Justification = "Literal needs to be added to dictionary.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "WebSocket", Scope = "member", Target = "System.ServiceModel.WebSockets.WebSocket.#Open()", Justification = "Literal needs to be added to dictionary.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "WebSocket", Scope = "member", Target = "System.ServiceModel.WebSockets.WebSocket.#MaxInputBufferSize", Justification = "Literal needs to be added to dictionary.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "MaxInputBufferSize", Scope = "member", Target = "System.ServiceModel.WebSockets.WebSocketProtocol.#EnsureRoomInBuffer()", Justification = "Literal needs to be added to dictionary.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "System.ServiceModel.WebSockets", Justification = "This namespace defines these types.")]

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Scope = "member", Target = "System.ServiceModel.WebSockets.MontenegroHybiUpgradeHelloHandshake00WebSocketProtocol.#StartWebSocketHandshake()", Justification = "Specification requirement")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "WebSocket", Scope = "member", Target = "System.ServiceModel.WebSockets.MontenegroHybiUpgradeHelloHandshake00WebSocketProtocol.#ValidateHandshakeResponseHeaders()", Justification = "Specification requirement")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "sec-websocket-location", Scope = "member", Target = "System.ServiceModel.WebSockets.MontenegroHybiUpgradeHelloHandshake00WebSocketProtocol.#ValidateHandshakeResponseHeaders()", Justification = "Specification requirement")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "sec-websocket-origin", Scope = "member", Target = "System.ServiceModel.WebSockets.MontenegroHybiUpgradeHelloHandshake00WebSocketProtocol.#ValidateHandshakeResponseHeaders()", Justification = "Specification requirement")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "sec-websocket-protocol", Scope = "member", Target = "System.ServiceModel.WebSockets.MontenegroHybiUpgradeHelloHandshake00WebSocketProtocol.#ValidateHandshakeResponseHeaders()", Justification = "Specification requirement")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", MessageId = "0#", Scope = "member", Target = "System.ServiceModel.WebSockets.WebSocket.#.ctor(System.String)", Justification = "This is destined as an interface with a web browser's javascript engine that does not understand Uri.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", MessageId = "0#", Scope = "member", Target = "System.ServiceModel.WebSockets.WebSocket.#.ctor(System.String,System.ServiceModel.WebSockets.WebSocketVersion)", Justification = "This is destined as an interface with a web browser's javascript engine that does not understand Uri.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", MessageId = "0#", Scope = "member", Target = "System.ServiceModel.WebSockets.WebSocket.#.ctor(System.String,System.ServiceModel.WebSockets.WebSocketVersion,System.String,System.String)", Justification = "This is destined as an interface with a web browser's javascript engine that does not understand Uri.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "WebSocket", Scope = "member", Target = "System.ServiceModel.WebSockets.WebSocket.#MaxInputBufferSize", Justification = "Specification requirement")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope = "member", Target = "System.ServiceModel.WebSockets.WebSocketEventArgs.#BinaryData", Justification = "Byte array represents the message buffer")]

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable", Scope = "type", Target = "System.ServiceModel.SMProtocol.SMStreamHeaders", Justification = "This type is not supposed for serialization")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable", Scope = "type", Target = "System.ServiceModel.SMProtocol.SMProtocolExeption", Justification = "This type is not supposed for serialization")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Scope = "member", Target = "System.ServiceModel.SMProtocol.FrameSerializer.#Serialize(System.ServiceModel.SMProtocol.SMFrames.BaseFrame)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Scope = "member", Target = "System.ServiceModel.SMProtocol.FrameSerializer.#Deserialize(System.Byte[])")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope = "member", Target = "System.ServiceModel.SMProtocol.SMData.#Data", Justification = "This is a raw buffer property.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope = "member", Target = "System.ServiceModel.SMProtocol.SMFrames.DataFrame.#Data", Justification = "This is a raw buffer property.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "System.Console.WriteLine(System.String)", Scope = "member", Target = "System.ServiceModel.SMProtocol.SMLogger.#LogDebug(System.String)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "System.Console.WriteLine(System.String)", Scope = "member", Target = "System.ServiceModel.SMProtocol.SMLogger.#LogError(System.String)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "System.Console.WriteLine(System.String)", Scope = "member", Target = "System.ServiceModel.SMProtocol.SMLogger.#LogInfo(System.String)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly", Scope = "type", Target = "System.ServiceModel.SMProtocol.SMFramesMonitor")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1816:CallGCSuppressFinalizeCorrectly", Scope = "member", Target = "System.ServiceModel.SMProtocol.SMFramesMonitor.#Dispose()")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly", Scope = "member", Target = "System.ServiceModel.SMProtocol.SMFramesMonitor.#Dispose()")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "System.Console.WriteLine(System.String,System.Object)", Scope = "member", Target = "System.ServiceModel.SMProtocol.SMProtocol.#OnSocketClose(System.Object,System.ServiceModel.WebSockets.WebSocketProtocolEventArgs)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope = "member", Target = "System.ServiceModel.WebSockets.WebSocketProtocolEventArgs.#BinaryData")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope = "member", Target = "ClientProtocol.ServiceModel.SMProtocol.MessageProcessing.CompressionDictionary.#Dictionary")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly", Scope = "type", Target = "ClientProtocol.ServiceModel.SMProtocol.MessageProcessing.CompressionProcessor")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1816:CallGCSuppressFinalizeCorrectly", Scope = "member", Target = "ClientProtocol.ServiceModel.SMProtocol.MessageProcessing.CompressionProcessor.#Dispose()")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly", Scope = "member", Target = "ClientProtocol.ServiceModel.SMProtocol.MessageProcessing.CompressionProcessor.#Dispose()")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1816:CallGCSuppressFinalizeCorrectly", Scope = "member", Target = "System.ServiceModel.Http2Protocol.SecureSocketProxy.#Dispose()")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2240:ImplementISerializableCorrectly", Scope = "type", Target = "System.ServiceModel.Http2Protocol.ProtocolHeaders")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2240:ImplementISerializableCorrectly", Scope = "type", Target = "System.ServiceModel.Http2Protocol.ProtocolExeption")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Scope = "member", Target = "System.ServiceModel.Http2Protocol.Http2Protocol.#CreateSocketByUri(System.Uri)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope = "member", Target = "System.ServiceModel.Http2Protocol.ProtocolData.#Data")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope = "member", Target = "System.ServiceModel.Http2Protocol.ProtocolFrames.DataFrame.#Data")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope = "member", Target = "ClientProtocol.ServiceModel.Http2Protocol.MessageProcessing.CompressionDictionary.#Dictionary")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1816:CallGCSuppressFinalizeCorrectly", Scope = "member", Target = "System.ServiceModel.Http2Protocol.ProtocolFramesMonitor.#Dispose()")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1816:CallGCSuppressFinalizeCorrectly", Scope = "member", Target = "ClientProtocol.ServiceModel.Http2Protocol.MessageProcessing.CompressionProcessor.#Dispose()")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly", Scope = "type", Target = "System.ServiceModel.Http2Protocol.SecureSocketProxy")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly", Scope = "type", Target = "System.ServiceModel.Http2Protocol.ProtocolFramesMonitor")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly", Scope = "type", Target = "ClientProtocol.ServiceModel.Http2Protocol.MessageProcessing.CompressionProcessor")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly", Scope = "member", Target = "System.ServiceModel.Http2Protocol.SecureSocketProxy.#Dispose()")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly", Scope = "member", Target = "System.ServiceModel.Http2Protocol.ProtocolFramesMonitor.#Dispose()")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly", Scope = "member", Target = "ClientProtocol.ServiceModel.Http2Protocol.MessageProcessing.CompressionProcessor.#Dispose()")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Scope = "member", Target = "System.ServiceModel.Http2Protocol.ProtocolFrames.ControlFrame.#SettingsHeaders")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Scope = "member", Target = "ClientProtocol.ServiceModel.Http2Protocol.ProtocolFrames.SettingsFrame.#KeyValuePairs")]
