// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
namespace Microsoft.Http2.Protocol
{
    /// <summary>
    /// Http status codes.
    /// </summary>
    public static class StatusCode
    {
            public const int Code500InternalServerError = 500;
            public const int Code200Ok = 200;
            public const int Code404NotFound = 404;
            public const int Code401Forbidden = 401;
            public const int Code100Continue = 100;
            public const int Code101SwitchingProtocols = 101;
            public const int Code501NotImplemented = 501;

            public const string Reason500InternalServerError = "Internal Server Error";
            public const string Reason100Continue = "Internal Server Error";
            public const string Reason200Ok = "OK";
            public const string Reason404NotFound = "Not Found";
            public const string Reason101SwitchingProtocols = "Switching protocols";
            public const string Reason501NotImplemented = "Not implemented";

            public static string GetReasonPhrase(int statusCode)
            {
                switch (statusCode)
                {
                    case Code100Continue: return Reason100Continue;
                    case Code200Ok: return Reason200Ok;
                    case Code404NotFound: return Reason404NotFound;
                    case Code500InternalServerError: return Reason500InternalServerError;
                    case Code101SwitchingProtocols: return Reason101SwitchingProtocols;
                    case Code501NotImplemented: return Reason501NotImplemented;
                    default:
                        return null;
                }
            }
    }
}
