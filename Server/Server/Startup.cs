using Owin;
using System.Web.Http;

namespace Server
{
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
