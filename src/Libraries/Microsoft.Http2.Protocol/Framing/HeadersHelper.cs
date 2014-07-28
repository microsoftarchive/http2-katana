// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.

using System;
using System.Collections.Generic;

namespace Microsoft.Http2.Protocol.Framing
{
    internal class HeadersHelper
    {
        static object concatLock = new object();
        static object splitLock = new object();

        /* 13 -> 8.1.2.3
        Header fields containing multiple values MUST be concatenated into a
        single value unless the ordering of that header field is known to be
        not significant. */
        public static void ConcatMultipleHeaders(HeadersList headers)
        {
            lock (concatLock)
            {
                for (int i = 0; i < headers.Count; i++)
                {
                    for (int j = i + 1; j < headers.Count; j++)
                    {
                        var anotherHeader = headers[j];
                        if (headers[i].Key == anotherHeader.Key && headers[i].Key[0] != ':')
                        {
                            string value = headers[i].Value + "\0" + anotherHeader.Value;
                            var newHeader = new KeyValuePair<string, string>(headers[i].Key, value);
                            headers[i] = newHeader;
                            headers.RemoveAt(j);
                            j--;
                        }
                    }
                }
            }
        }

        /* 13 -> 8.1.2.3
        After decompression, header fields that have values containing zero
        octets (0x0) MUST be split into multiple header fields before being
        processed. */
        public static void SplitMultipleHeaders(HeadersList headers)
        {
            lock (splitLock)
            {
                int count = headers.Count;
                for (int i = 0; i < count; i++)
                {
                    var header = headers[i];
                    if (header.Value.Contains("\0"))
                    {
                        string[] values = header.Value.Split(new[] { "\0" }, StringSplitOptions.None);
                        for (int j = 0; j < values.Length; j++)
                        {
                            string value = values[j];
                            headers.Insert(i + j, new KeyValuePair<string, string>(header.Key, value));
                        }
                        headers.RemoveAt(i + values.Length);
                        i = i + values.Length - 1;
                        count = count + values.Length - 1;
                    }
                }
            }            
        }
    }
}
