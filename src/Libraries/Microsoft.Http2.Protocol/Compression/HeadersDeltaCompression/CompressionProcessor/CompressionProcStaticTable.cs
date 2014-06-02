// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
using System;
using System.Collections.Generic;

namespace Microsoft.Http2.Protocol.Compression.HeadersDeltaCompression
{
    internal partial class CompressionProcessor
    {
        /* see spec 7 -> Appendix B.  Static Table
          +-------+-----------------------------+--------------+
          | Index | Header Name                 | Header Value |
          +-------+-----------------------------+--------------+
          | 1     | :authority                  |              |
          | 2     | :method                     | GET          |
          | 3     | :method                     | POST         |
          | 4     | :path                       | /            |
          | 5     | :path                       | /index.html  |
          | 6     | :scheme                     | http         |
          | 7     | :scheme                     | https        |
          | 8     | :status                     | 200          |
          | 9     | :status                     | 204          |
          | 10    | :status                     | 206          |
          | 11    | :status                     | 304          |
          | 12    | :status                     | 400          |
          | 13    | :status                     | 404          |
          | 14    | :status                     | 500          |
          | 15    | accept-charset              |              |
          | 16    | accept-encoding             |              |
          | 17    | accept-language             |              |
          | 18    | accept-ranges               |              |
          | 19    | accept                      |              |
          | 20    | access-control-allow-origin |              |
          | 21    | age                         |              |
          | 22    | allow                       |              |
          | 23    | authorization               |              |
          | 24    | cache-control               |              |
          | 25    | content-disposition         |              |
          | 26    | content-encoding            |              |
          | 27    | content-language            |              |
          | 28    | content-length              |              |
          | 29    | content-location            |              |
          | 30    | content-range               |              |
          | 31    | content-type                |              |
          | 32    | cookie                      |              |
          | 33    | date                        |              |
          | 34    | etag                        |              |
          | 35    | expect                      |              |
          | 36    | expires                     |              |
          | 37    | from                        |              |
          | 38    | host                        |              |
          | 39    | if-match                    |              |
          | 40    | if-modified-since           |              |
          | 41    | if-none-match               |              |
          | 42    | if-range                    |              |
          | 43    | if-unmodified-since         |              |
          | 44    | last-modified               |              |
          | 45    | link                        |              |
          | 46    | location                    |              |
          | 47    | max-forwards                |              |
          | 48    | proxy-authenticate          |              |
          | 49    | proxy-authorization         |              |
          | 50    | range                       |              |
          | 51    | referer                     |              |
          | 52    | refresh                     |              |
          | 53    | retry-after                 |              |
          | 54    | server                      |              |
          | 55    | set-cookie                  |              |
          | 56    | strict-transport-security   |              |
          | 57    | transfer-encoding           |              |
          | 58    | user-agent                  |              |
          | 59    | vary                        |              |
          | 60    | via                         |              |
          | 61    | www-authenticate            |              |
          +-------+-----------------------------+--------------+ */

        private readonly HeadersList _staticTable = new HeadersList( new []
            {
                    new KeyValuePair<string, string>(":authority", String.Empty),                               //1
                    new KeyValuePair<string, string>(":method", Verbs.Get.ToUpper()),                           //2
                    new KeyValuePair<string, string>(":method", Verbs.Post.ToUpper()),                          //3
                    new KeyValuePair<string, string>(":path", "/"),                                             //4
                    new KeyValuePair<string, string>(":path", "/index.html"),                                   //5
                    new KeyValuePair<string, string>(":scheme", "http"),                                        //6
                    new KeyValuePair<string, string>(":scheme", "https"),                                       //7
                    new KeyValuePair<string, string>(":status", "200"),                                         //8
                    new KeyValuePair<string, string>(":status", "204"),                                         //9
                    new KeyValuePair<string, string>(":status", "206"),                                         //10
                    new KeyValuePair<string, string>(":status", "304"),                                         //11
                    new KeyValuePair<string, string>(":status", "400"),                                         //12
                    new KeyValuePair<string, string>(":status", "404"),                                         //13
                    new KeyValuePair<string, string>(":status", "500"),                                         //14
                    new KeyValuePair<string, string>("accept-charset", String.Empty),                           //15
                    new KeyValuePair<string, string>("accept-encoding", String.Empty),                          //16
                    new KeyValuePair<string, string>("accept-language", String.Empty),                          //17
                    new KeyValuePair<string, string>("accept-ranges", String.Empty),                            //18
                    new KeyValuePair<string, string>("accept", String.Empty),                                   //19
                    new KeyValuePair<string, string>("access-control-allow-origin", String.Empty),              //20
                    new KeyValuePair<string, string>("age", String.Empty),                                      //21
                    new KeyValuePair<string, string>("allow", String.Empty),                                    //22
                    new KeyValuePair<string, string>("authorization", String.Empty),                            //23
                    new KeyValuePair<string, string>("cache-control", String.Empty),                            //24
                    new KeyValuePair<string, string>("content-disposition", String.Empty),                      //25
                    new KeyValuePair<string, string>("content-encoding", String.Empty),                         //26
                    new KeyValuePair<string, string>("content-language", String.Empty),                         //27
                    new KeyValuePair<string, string>("content-length", String.Empty),                           //28
                    new KeyValuePair<string, string>("content-location", String.Empty),                         //29
                    new KeyValuePair<string, string>("content-range", String.Empty),                            //30
                    new KeyValuePair<string, string>("content-type", String.Empty),                             //31   
                    new KeyValuePair<string, string>("cookie", String.Empty),                                   //32
                    new KeyValuePair<string, string>("date", String.Empty),                                     //33
                    new KeyValuePair<string, string>("etag", String.Empty),                                     //34   
                    new KeyValuePair<string, string>("expect", String.Empty),                                   //35
                    new KeyValuePair<string, string>("expires", String.Empty),                                  //36
                    new KeyValuePair<string, string>("from", String.Empty),                                     //37
                    new KeyValuePair<string, string>("host", String.Empty),                                     //38
                    new KeyValuePair<string, string>("if-match", String.Empty),                                 //39
                    new KeyValuePair<string, string>("if-modified-since", String.Empty),                        //40
                    new KeyValuePair<string, string>("if-none-match", String.Empty),                            //41
                    new KeyValuePair<string, string>("if-range", String.Empty),                                 //42 
                    new KeyValuePair<string, string>("if-unmodified-since", String.Empty),                      //43
                    new KeyValuePair<string, string>("last-modified", String.Empty),                            //44
                    new KeyValuePair<string, string>("link", String.Empty),                                     //45
                    new KeyValuePair<string, string>("location", String.Empty),                                 //46
                    new KeyValuePair<string, string>("max-forwards", String.Empty),                             //47
                    new KeyValuePair<string, string>("proxy-authenticate", String.Empty),                       //48
                    new KeyValuePair<string, string>("proxy-authorization", String.Empty),                      //49
                    new KeyValuePair<string, string>("range", String.Empty),                                    //50
                    new KeyValuePair<string, string>("referer", String.Empty),                                  //51
                    new KeyValuePair<string, string>("refresh", String.Empty),                                  //52
                    new KeyValuePair<string, string>("retry-after", String.Empty),                              //53
                    new KeyValuePair<string, string>("server", String.Empty),                                   //54
                    new KeyValuePair<string, string>("set-cookie", String.Empty),                               //55
                    new KeyValuePair<string, string>("strict-transport-security", String.Empty),                //56
                    new KeyValuePair<string, string>("transfer-encoding", String.Empty),                        //57
                    new KeyValuePair<string, string>("user-agent", String.Empty),                               //58
                    new KeyValuePair<string, string>("vary", String.Empty),                                     //59
                    new KeyValuePair<string, string>("via", String.Empty),                                      //60
                    new KeyValuePair<string, string>("www-authenticate", String.Empty),                         //61
            });
    }
}