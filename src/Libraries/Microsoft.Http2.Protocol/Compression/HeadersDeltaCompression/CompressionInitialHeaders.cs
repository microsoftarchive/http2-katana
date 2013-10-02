using System;
using System.Collections.Generic;

namespace Microsoft.Http2.Protocol.Compression.HeadersDeltaCompression
{
    internal static class CompressionInitialHeaders
    {
        /* +-------+---------------------+--------------+
           | Index | Header Name         | Header Value |
           +-------+---------------------+--------------+
           | 0     | :scheme             | http         |
           | 1     | :scheme             | https        |
           | 2     | :host               |              |
           | 3     | :path               | /            |
           | 4     | :method             | GET          |
           | 5     | accept              |              |
           | 6     | accept-charset      |              |
           | 7     | accept-encoding     |              |
           | 8     | accept-language     |              |
           | 9     | cookie              |              |
           | 10    | if-modified-since   |              |
           | 11    | user-agent          |              |
           | 12    | referer             |              |
           | 13    | authorization       |              |
           | 14    | allow               |              |
           | 15    | cache-control       |              |
           | 16    | connection          |              |
           | 17    | content-length      |              |
           | 18    | content-type        |              |
           | 19    | date                |              |
           | 20    | expect              |              |
           | 21    | from                |              |
           | 22    | if-match            |              |
           | 23    | if-none-match       |              |
           | 24    | if-range            |              |
           | 25    | if-unmodified-since |              |
           | 26    | max-forwards        |              |
           | 27    | proxy-authorization |              |
           | 28    | range               |              |
           | 29    | via                 |              |
           +-------+---------------------+--------------+*/

        private static readonly KeyValuePair<string, string>[] requestInitialHeaders =
        {
            new KeyValuePair<string, string>(":scheme", "https"),                   //0
            new KeyValuePair<string, string>(":scheme", "http"),                    //1
            new KeyValuePair<string, string>(":host", String.Empty),                //2
            new KeyValuePair<string, string>(":path", "/"),                         //3
            new KeyValuePair<string, string>(":method", "GET"),                     //4
            new KeyValuePair<string, string>("accept", String.Empty),               //5
            new KeyValuePair<string, string>("accept-charset", String.Empty),       //6
            new KeyValuePair<string, string>("accept-encoding", String.Empty),      //7
            new KeyValuePair<string, string>("accept-language", String.Empty),      //8
            new KeyValuePair<string, string>("cookie", String.Empty),               //9
            new KeyValuePair<string, string>("if-modified-since", String.Empty),    //10
            new KeyValuePair<string, string>("user-agent", String.Empty),           //11
            new KeyValuePair<string, string>("referer", String.Empty),              //12
            new KeyValuePair<string, string>("authorization", String.Empty),        //13
            new KeyValuePair<string, string>("allow", String.Empty),                //14
            new KeyValuePair<string, string>("cache-control", String.Empty),        //15
            new KeyValuePair<string, string>("connection", String.Empty),           //16
            new KeyValuePair<string, string>("content-length", String.Empty),       //17
            new KeyValuePair<string, string>("content-type", String.Empty),         //18
            new KeyValuePair<string, string>("date", String.Empty),                 //19
            new KeyValuePair<string, string>("expect", String.Empty),               //20
            new KeyValuePair<string, string>("from", String.Empty),                 //21
            new KeyValuePair<string, string>("if-match", String.Empty),             //22
            new KeyValuePair<string, string>("if-none-match", String.Empty),        //23
            new KeyValuePair<string, string>("if-range", String.Empty),             //24
            new KeyValuePair<string, string>("if-unmodified-since", String.Empty),  //25
            new KeyValuePair<string, string>("max-forwards", String.Empty),         //26
            new KeyValuePair<string, string>("proxy-authorization", String.Empty),  //27
            new KeyValuePair<string, string>("range", String.Empty),                //28
            new KeyValuePair<string, string>("via", String.Empty),                  //29
        };

        /*+-------+-----------------------------+--------------+
          | Index | Header Name                 | Header Value |
          +-------+-----------------------------+--------------+
          | 0     | :status                     | 200          |
          | 1     | age                         |              |
          | 2     | cache-control               |              |
          | 3     | content-length              |              |
          | 4     | content-type                |              |
          | 5     | date                        |              |
          | 6     | etag                        |              |
          | 7     | expires                     |              |
          | 8     | last-modified               |              |
          | 9     | server                      |              |
          | 10    | set-cookie                  |              |
          | 11    | vary                        |              |
          | 12    | via                         |              |
          | 13    | access-control-allow-origin |              |
          | 14    | accept-ranges               |              |
          | 15    | allow                       |              |
          | 16    | connection                  |              |
          | 17    | content-disposition         |              |
          | 18    | content-encoding            |              |
          | 19    | content-language            |              |
          | 20    | content-location            |              |
          | 21    | content-range               |              |
          | 22    | link                        |              |
          | 23    | location                    |              |
          | 24    | proxy-authenticate          |              |
          | 25    | refresh                     |              |
          | 26    | retry-after                 |              |
          | 27    | strict-transport-security   |              |
          | 28    | transfer-encoding           |              |
          | 29    | www-authenticate            |              |
          +-------+-----------------------------+--------------+*/

        private static readonly KeyValuePair<string,string>[] responseInitialHeaders =
        {
            new KeyValuePair<string, string>(":status", "200"),                             //0
            new KeyValuePair<string, string>("age", String.Empty),                          //1
            new KeyValuePair<string, string>("cache-control", String.Empty),                //2
            new KeyValuePair<string, string>("content-length",String.Empty),                //3
            new KeyValuePair<string, string>("content-type", String.Empty),                 //4
            new KeyValuePair<string, string>("date", String.Empty),                         //5
            new KeyValuePair<string, string>("etag", String.Empty),                         //6
            new KeyValuePair<string, string>("expires", String.Empty),                      //7
            new KeyValuePair<string, string>("last-modified", String.Empty),                //8
            new KeyValuePair<string, string>("server", String.Empty),                       //9
            new KeyValuePair<string, string>("set-cookie", String.Empty),                   //10
            new KeyValuePair<string, string>("vary", String.Empty),                         //11
            new KeyValuePair<string, string>("via", String.Empty),                          //12
            new KeyValuePair<string, string>("access-control-allow-origin", String.Empty),  //13
            new KeyValuePair<string, string>("accept-ranges", String.Empty),                //14
            new KeyValuePair<string, string>("allow", String.Empty),                        //15
            new KeyValuePair<string, string>("connection", String.Empty),                   //16
            new KeyValuePair<string, string>("content-disposition", String.Empty),          //17
            new KeyValuePair<string, string>("content-encoding", String.Empty),             //18
            new KeyValuePair<string, string>("content-language", String.Empty),             //19
            new KeyValuePair<string, string>("content-location", String.Empty),             //20
            new KeyValuePair<string, string>("content-range", String.Empty),                //21
            new KeyValuePair<string, string>("link", String.Empty),                         //22
            new KeyValuePair<string, string>("location", String.Empty),                     //23
            new KeyValuePair<string, string>("proxy-authenticate", String.Empty),           //24
            new KeyValuePair<string, string>("refresh", String.Empty),                      //25
            new KeyValuePair<string, string>("retry-after", String.Empty),                  //26
            new KeyValuePair<string, string>("strict-transport-security", String.Empty),    //27
            new KeyValuePair<string, string>("transfer-encoding", String.Empty),            //28
            new KeyValuePair<string, string>("www-authenticate", String.Empty)              //29
        };

        public static HeadersList RequestInitialHeaders
        {
            get { return new HeadersList(requestInitialHeaders); }
        }

        public static HeadersList ResponseInitialHeaders
        {
            get { return new HeadersList(responseInitialHeaders); }
        }
    }
}
