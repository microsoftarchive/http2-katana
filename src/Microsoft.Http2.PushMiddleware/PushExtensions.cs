using Microsoft.Http2.Push;

namespace Owin
{
    public static class PushExtensions
    {
        public static IAppBuilder UsePush(this IAppBuilder builder)
        {
            return builder.Use(typeof(PushMiddleware));
        }
    }
}
