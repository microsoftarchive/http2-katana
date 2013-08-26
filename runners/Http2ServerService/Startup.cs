using System.Web.Http;
using Owin;

namespace Http2ServerService
{
    public class Startup
    {
        /// <summary>
        /// This class is used for building katana stack in the Http2ServerService.
        /// </summary>
        /// <param name="builder">This object is used for building katana stack</param>
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
