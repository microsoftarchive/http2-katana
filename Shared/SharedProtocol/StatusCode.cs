namespace SharedProtocol
{
    public class StatusCode
    {
            public const int Code500InternalServerError = 500;
            public const int Code200Ok = 200;
            public const int Code404NotFound = 404;
            public const int Code401Forbidden = 401;
            public const int Code100Continue = 100;

            public const string Reason500InternalServerError = "Internal Server Error";

            internal static string GetReasonPhrase(int statusCode)
            {
                switch (statusCode)
                {
                    case Code100Continue: return "Continue";
                    case Code200Ok: return "OK";
                    case Code404NotFound: return "Not Found";
                    case Code500InternalServerError: return Reason500InternalServerError;

                    default:
                        return null;
                }
            }
    }
}
