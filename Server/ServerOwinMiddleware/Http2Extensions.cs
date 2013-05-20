using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Owin;
using ServerOwinMiddleware;

namespace Owin
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public static class Http2Extensions
    {
        // Upgrades incoming requests to HTTP/2.0 and passes them on to the normal pipeline.
        public static IAppBuilder UseHttp2(this IAppBuilder builder)
        {
            return builder.Use(typeof(Http2Middleware));
        }

        // Upgrades incoming requests to HTTP/2.0 and passes them on to a separate pipeline.
        public static IAppBuilder UseHttp2(this IAppBuilder builder, Action<IAppBuilder> configBranch)
        {
            IAppBuilder builder1 = builder.New();
            configBranch(builder1);
            return builder.Use(typeof(Http2Middleware), builder1.Build(typeof(AppFunc)));
        }
    }
}
