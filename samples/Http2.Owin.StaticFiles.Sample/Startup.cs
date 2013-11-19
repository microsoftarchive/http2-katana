using Microsoft.Http2.Owin.Middleware;
using Owin;

namespace Http2.Owin.StaticFiles.Sample
{
    /// <summary>
    /// This class is used for building katana stack.
    /// See owin spec: http://owin.org/spec/owin-1.0.0.html
    /// </summary>
    public class Startup
    {
        public void Configuration(IAppBuilder builder)
        {
            builder.UseHttp2();
            builder.UseStaticFiles("root");
        }
    }
}
