using System.Configuration;
using Microsoft.Owin.Hosting;
using SocketServer;

namespace Server
{
    class Program
    {
        static void Main()
        {
            bool isSecure = ConfigurationManager.AppSettings["useSecurePort"] == "true";
            string connectString = isSecure
                                       ? ConfigurationManager.AppSettings["secureAddress"]
                                       : ConfigurationManager.AppSettings["unsecureAddress"];

            var startOpt = new StartOptions(connectString)
                {
                    ServerFactory = typeof (SocketServerFactory).ToString(),
                };
            
            // Start socket server depends on chosen port
            using (WebApp.Start<Startup>(startOpt))
            {

            }
        }
    }
}
