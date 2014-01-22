// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
using System;
using System.Collections.Generic;

namespace Microsoft.Http2.Protocol.Extensions
{
    public static class DictionaryExtenstions
    {
        //Develop template
        public static void AddRange(this IDictionary<string, object> dest, IDictionary<string, object> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("Source is null");
            }

            foreach (var item in source)
            {
                dest.Add(item.Key, item.Value);
            }
        }

        public static void AddRange(this IDictionary<string, string> dest, IDictionary<string, string> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("Source is null");
            }

            foreach (var item in source)
            {
                dest.Add(item.Key, item.Value);
            }
        }
    }
}
