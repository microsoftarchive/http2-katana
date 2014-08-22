// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.

using System.Collections.Specialized;
using System.Configuration;

namespace Http2.TestClient
{
    internal class ClientOptions
    {
        private static readonly NameValueCollection AppSettings = ConfigurationManager.AppSettings;

        public const int DefaultSecurePort = 8443;

        public static bool IsDirectEnabled
        {
            get { return AppSettings[Strings.DirectEnabled] == "true"; }
        }

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

        public static bool IsTestModeEnabled
        {
            get { return AppSettings[Strings.TestModeEnabled] == "true"; }
        }
    }
}
