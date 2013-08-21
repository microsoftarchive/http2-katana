using System.Web.Http;
using Owin;
using Microsoft.Owin;

namespace Server
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
            ConfigureWebApi(builder);
        }

        private void ConfigureWebApi(IAppBuilder builder)
        {
            var config = new HttpConfiguration();
            builder.UseHttpServer(config);
        }
    }
}
