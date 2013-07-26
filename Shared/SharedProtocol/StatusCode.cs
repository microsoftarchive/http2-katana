namespace SharedProtocol
{
    //TODO Rework class (more status codes + refactor existing)
    public class StatusCode
    {
            public const int Code500InternalServerError = 500;

            public const string Reason500InternalServerError = "Internal Server Error";

            internal static string GetReasonPhrase(int statusCode)
            {
                switch (statusCode)
                {
                    case 100: return "Continue";
                    case 200: return "OK";
                    case 404: return "Not Found";
                    case Code500InternalServerError: return Reason500InternalServerError;

                    default:
                        return null;
                }
            }
    }
}
