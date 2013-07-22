using System.Configuration;
using Microsoft.Owin.Hosting;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            bool isSecure = ConfigurationManager.AppSettings["useSecurePort"] == "true";
            string connectString = isSecure
                                       ? ConfigurationManager.AppSettings["secureAddress"]
                                       : ConfigurationManager.AppSettings["unsecureAddress"];

            // Start socket server depends on chosen port
            using (WebApplication.Start<Startup>(options =>
                {
                    options.Url = connectString;
                    options.Server = "SocketServer";
                }))
            {

            }
        }
    }
}
