using System.Web.Http;
using Owin;

namespace Http2.Owin.WebApi.Sample
{
    /// <summary>
    /// This class is used for building katana stack.
    /// See owin spec: http://owin.org/spec/owin-1.0.0.html
    /// </summary>
    public class Startup
    {
        // This code configures Web API. The Startup class is specified as a type
        // parameter in the WebApp.Start method.
        public void Configuration(IAppBuilder appBuilder)
        {
            // Configure Web API for self-host. 
            HttpConfiguration config = new HttpConfiguration();
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            // config.Formatters.XmlFormatter.UseXmlSerializer = true;
            // config.Formatters.Remove(config.Formatters.JsonFormatter);
            config.Formatters.JsonFormatter.UseDataContractJsonSerializer = true;

            appBuilder.UseHttp2();
            appBuilder.UseWebApi(config);
        }
    }
}
