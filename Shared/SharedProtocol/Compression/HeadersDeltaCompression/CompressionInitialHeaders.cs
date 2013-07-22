using System;
using System.Collections.Generic;
using SharedProtocol.Compression.HeadersDeltaCompression;

namespace SharedProtocol.Compression.Http2DeltaHeadersCompression
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
              | 4     | :method             | get          |
              | 5     | accept              |              |
              | 6     | accept-charset      |              |
              | 7     | accept-encoding     |              |
              | 8     | accept-language     |              |
              | 9     | cookie              |              |
              | 10    | if-modified-since   |              |
              | 11    | keep-alive          |              |
              | 12    | user-agent          |              |
              | 13    | proxy-connection    |              |
              | 14    | referer             |              |
              | 15    | accept-datetime     |              |
              | 16    | authorization       |              |
              | 17    | allow               |              |
              | 18    | cache-control       |              |
              | 19    | connection          |              |
              | 20    | content-length      |              |
              | 21    | content-md5         |              |
              | 22    | content-type        |              |
              | 23    | date                |              |
              | 24    | expect              |              |
              | 25    | from                |              |
              | 26    | if-match            |              |
              | 27    | if-none-match       |              |
              | 28    | if-range            |              |
              | 29    | if-unmodified-since |              |
              | 30    | max-forwards        |              |
              | 31    | pragma              |              |
              | 32    | proxy-authorization |              |
              | 33    | range               |              |
              | 34    | te                  |              |
              | 35    | upgrade             |              |
              | 36    | via                 |              |
              | 37    | warning             |              |
              +-------+---------------------+--------------+*/

        private static readonly SizedHeadersList requestInitialHeaders = new SizedHeadersList
            {
                new KeyValuePair<string, string>(":scheme", "https"),
                new KeyValuePair<string, string>(":scheme", "http"),
                new KeyValuePair<string, string>(":host", String.Empty),
                new KeyValuePair<string, string>(":path", "/"),
                new KeyValuePair<string, string>(":method", "get"),
                new KeyValuePair<string, string>("accept", String.Empty),
                new KeyValuePair<string, string>("accept-charset", String.Empty),
                new KeyValuePair<string, string>("accept-encoding", String.Empty),
                new KeyValuePair<string, string>("accept-language", String.Empty),
                new KeyValuePair<string, string>("cookie", String.Empty),
                new KeyValuePair<string, string>("if-modified-since", String.Empty),
                new KeyValuePair<string, string>("keep-alive", String.Empty),
                new KeyValuePair<string, string>("user-agent", String.Empty),
                new KeyValuePair<string, string>("proxy-connection", String.Empty),
                new KeyValuePair<string, string>("referer", String.Empty),
                new KeyValuePair<string, string>("accept-datetime", String.Empty),
                new KeyValuePair<string, string>("authorization", String.Empty),
                new KeyValuePair<string, string>("allow", String.Empty),
                new KeyValuePair<string, string>("cache-control", String.Empty),
                new KeyValuePair<string, string>("connection", String.Empty),
                new KeyValuePair<string, string>("content-length", String.Empty),
                new KeyValuePair<string, string>("content-md5", String.Empty),
                new KeyValuePair<string, string>("content-type", String.Empty),
                new KeyValuePair<string, string>("date", String.Empty),
                new KeyValuePair<string, string>("expect", String.Empty),
                new KeyValuePair<string, string>("from", String.Empty),//
                new KeyValuePair<string, string>("if-match", String.Empty),
                new KeyValuePair<string, string>("if-none-match", String.Empty),
                new KeyValuePair<string, string>("if-range", String.Empty),
                new KeyValuePair<string, string>("if-unmodified-since", String.Empty),
                new KeyValuePair<string, string>("max-forwards", String.Empty),
                new KeyValuePair<string, string>("pragma", String.Empty),
                new KeyValuePair<string, string>("proxy-authorization", String.Empty),
                new KeyValuePair<string, string>("range", String.Empty),
                new KeyValuePair<string, string>("te", String.Empty),
                new KeyValuePair<string, string>("upgrade", String.Empty),
                new KeyValuePair<string, string>("via", String.Empty),
                new KeyValuePair<string, string>("warning", String.Empty),
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
          | 21    | content-md5                 |              |
          | 22    | content-range               |              |
          | 23    | link                        |              |
          | 24    | location                    |              |
          | 25    | p3p                         |              |
          | 26    | pragma                      |              |
          | 27    | proxy-authenticate          |              |
          | 28    | refresh                     |              |
          | 29    | retry-after                 |              |
          | 30    | strict-transport-security   |              |
          | 31    | trailer                     |              |
          | 32    | transfer-encoding           |              |
          | 33    | warning                     |              |
          | 34    | www-authenticate            |              |
          +-------+-----------------------------+--------------+*/

        private static readonly SizedHeadersList responseInitialHeaders = new SizedHeadersList
            {
                new KeyValuePair<string, string>(":status", "200"),
                new KeyValuePair<string, string>("age", String.Empty),
                new KeyValuePair<string, string>("cache-control", String.Empty),
                new KeyValuePair<string, string>("content-length",String.Empty),
                new KeyValuePair<string, string>("content-type", String.Empty),
                new KeyValuePair<string, string>("date", String.Empty),
                new KeyValuePair<string, string>("etag", String.Empty),
                new KeyValuePair<string, string>("expires", String.Empty),
                new KeyValuePair<string, string>("last-modified", String.Empty),//
                new KeyValuePair<string, string>("server", String.Empty),
                new KeyValuePair<string, string>("set-cookie", String.Empty),
                new KeyValuePair<string, string>("vary", String.Empty),
                new KeyValuePair<string, string>("via", String.Empty),
                new KeyValuePair<string, string>("access-control-allow-origin", String.Empty),
                new KeyValuePair<string, string>("accept-ranges", String.Empty),
                new KeyValuePair<string, string>("allow", String.Empty),//
                new KeyValuePair<string, string>("connection", String.Empty),
                new KeyValuePair<string, string>("content-disposition", String.Empty),
                new KeyValuePair<string, string>("content-encoding", String.Empty),
                new KeyValuePair<string, string>("content-language", String.Empty),
                new KeyValuePair<string, string>("content-location", String.Empty),
                new KeyValuePair<string, string>("content-md5", String.Empty),
                new KeyValuePair<string, string>("content-range", String.Empty),//
                new KeyValuePair<string, string>("link", String.Empty),
                new KeyValuePair<string, string>("location", String.Empty),
                new KeyValuePair<string, string>("p3p", String.Empty),
                new KeyValuePair<string, string>("pragma", String.Empty),
                new KeyValuePair<string, string>("proxy-authenticate", String.Empty),
                new KeyValuePair<string, string>("refresh", String.Empty),//
                new KeyValuePair<string, string>("retry-after", String.Empty),
                new KeyValuePair<string, string>("strict-transport-security", String.Empty),
                new KeyValuePair<string, string>("trailer", String.Empty),
                new KeyValuePair<string, string>("transfer-encoding", String.Empty),
                new KeyValuePair<string, string>("warning", String.Empty),
                new KeyValuePair<string, string>("www-authenticate", String.Empty),
            };

        public static SizedHeadersList RequestInitialHeaders
        {
            get { return new SizedHeadersList(requestInitialHeaders); }
        }

        public static SizedHeadersList ResponseInitialHeaders
        {
            get { return new SizedHeadersList(responseInitialHeaders); }
        }
    }
}
