// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
namespace Owin
{
    public static class CommonOwinKeys
    {
        public const string RequestHeaders = "owin.RequestHeaders";
        public const string ResponseHeaders = "owin.ResponseHeaders";

        public const string OwinCallCancelled = "owin.CallCancelled";
        public const string OwinVersion = "1.0";

        public const string OpaqueUpgrade = "opaque.Upgrade";
        public const string OpaqueStream = "opaque.Stream";
        public const string OpaqueVersion = "opaque.Version";
        public const string OpaqueCallCancelled = "opaque.CallCancelled";

        public const string ServerPushFunc = "server.Push";
        public const string EnableServerPush = "enable.push";

        public const string AdditionalInfo = "push.add.info";
        public const string AddVertex = "push.add.vertex";
        public const string RemoveVertex = "push.remove.vertex";
    }
}
