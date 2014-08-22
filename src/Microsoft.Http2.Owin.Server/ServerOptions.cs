// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.

using System;
using System.Configuration;
using System.Collections.Specialized;

namespace Microsoft.Http2.Owin.Server
{
    public static class ServerOptions
    {
        private static readonly NameValueCollection AppSettings = ConfigurationManager.AppSettings;

        public const int DefaultSecurePort = 8443;
        public const int DefaultUnsecurePort = 8080;

        public static int SecurePort
        {
            get
            {
                int result;
                if (int.TryParse(AppSettings[Strings.SecurePort], out result))
                {
                    return result;
                }

                return DefaultSecurePort;
            }
        }

        public static int UnsecurePort
        {
            get
            {
                int result; 
                if (int.TryParse(AppSettings[Strings.UnsecurePort], out result))
                {
                    return result;
                }

                return DefaultUnsecurePort;
            }
        }

        public static bool UseSecureAddress
        {
            get { return AppSettings[Strings.UseSecureAddress] == "true"; }
        }

        public static string ServerName
        {
            get { return AppSettings[Strings.ServerName]; }
        }

        public static string Address
        {
            get
            {
                var scheme = UseSecureAddress ? Strings.Https : Strings.Http;
                var port = UseSecureAddress ? SecurePort : UnsecurePort;              
                return FormatAddress(scheme, ServerName, port);
            }
        }

        public static string SecureAddress
        {
            get {return FormatAddress(Strings.Https, ServerName, SecurePort); }
        }

        public static string UnsecureAddress
        {
            get {return FormatAddress(Strings.Http, ServerName, UnsecurePort); }
        }

        private static string FormatAddress(string scheme, string serverName, int port)
        {
            return String.Format("{0}://{1}:{2}/", scheme, serverName, port);
        }

        public static bool IsDirectEnabled
        {
            get { return AppSettings[Strings.DirectEnabled] == "true"; }
        }

        public static string FileName
        {
            get { return AppSettings[Strings.FileName]; }
        }
    }
}
