namespace SharedProtocol
{
    //TODO move it from the http2 proto
    /// <summary>
    /// Http status codes.
    /// </summary>
    public class StatusCode
    {
            public const int Code500InternalServerError = 500;
            public const int Code200Ok = 200;
            public const int Code404NotFound = 404;
            public const int Code401Forbidden = 401;
            public const int Code100Continue = 100;
            public const int Code101SwitchingProtocols = 101;

            public const string Reason500InternalServerError = "Internal Server Error";
            public const string Reason100Continue = "Internal Server Error";
            public const string Reason200Ok = "OK";
            public const string Reason404NotFound = "Not Found";
            public const string Reason101SwitchingProtocols = "Switching protocols";

            internal static string GetReasonPhrase(int statusCode)
            {
                switch (statusCode)
                {
                    case Code100Continue: return Reason100Continue;
                    case Code200Ok: return Reason200Ok;
                    case Code404NotFound: return Reason404NotFound;
                    case Code500InternalServerError: return Reason500InternalServerError;
                    case Code101SwitchingProtocols: return Reason101SwitchingProtocols;
                    default:
                        return null;
                }
            }
    }
}
