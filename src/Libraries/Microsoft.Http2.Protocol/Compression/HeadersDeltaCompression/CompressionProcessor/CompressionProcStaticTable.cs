using System;
using System.Collections.Generic;

namespace Microsoft.Http2.Protocol.Compression.HeadersDeltaCompression
{
    internal partial class CompressionProcessor
    {
        /*+-------+-----------------------------+--------------+
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
          | 9     | :status                     | 500          |
          | 10    | :status                     | 404          |
          | 11    | :status                     | 403          |
          | 12    | :status                     | 400          |
          | 13    | :status                     | 401          |
          | 14    | accept-charset              |              |
          | 15    | accept-encoding             |              |
          | 16    | accept-language             |              |
          | 17    | accept-ranges               |              |
          | 18    | accept                      |              |
          | 19    | access-control-allow-origin |              |
          | 20    | age                         |              |
          | 21    | allow                       |              |
          | 22    | authorization               |              |
          | 23    | cache-control               |              |
          | 24    | content-disposition         |              |
          | 25    | content-encoding            |              |
          | 26    | content-language            |              |
          | 27    | content-length              |              |
          | 28    | content-location            |              |
          | 29    | content-range               |              |
          | 30    | content-type                |              |
          | 31    | cookie                      |              |
          | 32    | date                        |              |
          | 33    | etag                        |              |
          | 34    | expect                      |              |
          | 35    | expires                     |              |
          | 36    | from                        |              |
          | 37    | host                        |              |
          | 38    | if-match                    |              |
          | 39    | if-modified-since           |              |
          | 40    | if-none-match               |              |
          | 41    | if-range                    |              |
          | 42    | if-unmodified-since         |              |
          | 43    | last-modified               |              |
          | 44    | link                        |              |
          | 45    | location                    |              |
          | 46    | max-forwards                |              |
          | 47    | proxy-authenticate          |              |
          | 48    | proxy-authorization         |              |
          | 49    | range                       |              |
          | 50    | referer                     |              |
          | 51    | refresh                     |              |
          | 52    | retry-after                 |              |
          | 53    | server                      |              |
          | 54    | set-cookie                  |              |
          | 55    | strict-transport-security   |              |
          | 56    | transfer-encoding           |              |
          | 57    | user-agent                  |              |
          | 58    | vary                        |              |
          | 59    | via                         |              |
          | 60    | www-authenticate            |              |
          +-------+-----------------------------+--------------+*/

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
                    new KeyValuePair<string, string>(":status", "500"),                                         //9
                    new KeyValuePair<string, string>(":status", "404"),                                         //10
                    new KeyValuePair<string, string>(":status", "403"),                                         //11
                    new KeyValuePair<string, string>(":status", "400"),                                         //12
                    new KeyValuePair<string, string>(":status", "401"),                                         //13
                    new KeyValuePair<string, string>("accept-charset", String.Empty),                           //14
                    new KeyValuePair<string, string>("accept-encoding", String.Empty),                          //15
                    new KeyValuePair<string, string>("accept-language", String.Empty),                          //16
                    new KeyValuePair<string, string>("accept-ranges", String.Empty),                            //17
                    new KeyValuePair<string, string>("accept", String.Empty),                                   //18
                    new KeyValuePair<string, string>("access-control-allow-origin", String.Empty),              //19
                    new KeyValuePair<string, string>("age", String.Empty),                                      //20
                    new KeyValuePair<string, string>("allow", String.Empty),                                    //21
                    new KeyValuePair<string, string>("authorization", String.Empty),                            //22
                    new KeyValuePair<string, string>("cache-control", String.Empty),                            //23
                    new KeyValuePair<string, string>("content-disposition", String.Empty),                      //24
                    new KeyValuePair<string, string>("content-encoding", String.Empty),                         //25
                    new KeyValuePair<string, string>("content-language", String.Empty),                         //26
                    new KeyValuePair<string, string>("content-length", String.Empty),                           //27
                    new KeyValuePair<string, string>("content-location", String.Empty),                         //28
                    new KeyValuePair<string, string>("content-range", String.Empty),                            //29
                    new KeyValuePair<string, string>("content-type", String.Empty),                             //30   
                    new KeyValuePair<string, string>("cookie", String.Empty),                                   //31
                    new KeyValuePair<string, string>("date", String.Empty),                                     //32
                    new KeyValuePair<string, string>("etag", String.Empty),                                     //33   
                    new KeyValuePair<string, string>("expect", String.Empty),                                   //34
                    new KeyValuePair<string, string>("expires", String.Empty),                                  //35
                    new KeyValuePair<string, string>("from", String.Empty),                                     //36
                    new KeyValuePair<string, string>("host", String.Empty),                                     //37
                    new KeyValuePair<string, string>("if-match", String.Empty),                                 //38
                    new KeyValuePair<string, string>("if-modified-since", String.Empty),                        //39
                    new KeyValuePair<string, string>("if-none-match", String.Empty),                            //40
                    new KeyValuePair<string, string>("if-range", String.Empty),                                 //41 
                    new KeyValuePair<string, string>("if-unmodified-since", String.Empty),                      //42
                    new KeyValuePair<string, string>("last-modified", String.Empty),                            //43
                    new KeyValuePair<string, string>("link", String.Empty),                                     //44
                    new KeyValuePair<string, string>("location", String.Empty),                                 //45
                    new KeyValuePair<string, string>("max-forwards", String.Empty),                             //46
                    new KeyValuePair<string, string>("proxy-authenticate", String.Empty),                       //47
                    new KeyValuePair<string, string>("proxy-authorization", String.Empty),                      //48
                    new KeyValuePair<string, string>("range", String.Empty),                                    //49
                    new KeyValuePair<string, string>("referer", String.Empty),                                  //50
                    new KeyValuePair<string, string>("refresh", String.Empty),                                  //51
                    new KeyValuePair<string, string>("retry-after", String.Empty),                              //52
                    new KeyValuePair<string, string>("server", String.Empty),                                   //53
                    new KeyValuePair<string, string>("set-cookie", String.Empty),                               //54
                    new KeyValuePair<string, string>("strict-transport-security", String.Empty),                //55
                    new KeyValuePair<string, string>("transfer-encoding", String.Empty),                        //56
                    new KeyValuePair<string, string>("user-agent", String.Empty),                               //57
                    new KeyValuePair<string, string>("vary", String.Empty),                                     //58
                    new KeyValuePair<string, string>("via", String.Empty),                                      //59
                    new KeyValuePair<string, string>("www-authenticate", String.Empty),                         //60
            });
    }
}