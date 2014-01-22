// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
using Microsoft.Http2.Owin.Middleware;

namespace Owin
{
    public static class Http2Extensions
    {
        // Upgrades incoming requests to HTTP/2.0 and passes them on to the normal pipeline.
        public static IAppBuilder UseHttp2(this IAppBuilder builder)
        {
            return builder.Use(typeof(Http2Middleware));
        }
    }
}