using Owin;
using Owin.Types;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
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
            HttpConfiguration config = new HttpConfiguration();
            config.Routes.MapHttpRoute("Default", "{controller}/{customerID}", new { controller = "Customer", customerID = RouteParameter.Optional });

            // config.Formatters.XmlFormatter.UseXmlSerializer = true;
            // config.Formatters.Remove(config.Formatters.JsonFormatter);
            config.Formatters.JsonFormatter.UseDataContractJsonSerializer = true;

            builder.UseHttpServer(config);
        }
    }
}
