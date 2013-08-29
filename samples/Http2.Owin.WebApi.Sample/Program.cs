using System;
using Microsoft.Owin.Hosting;
using SocketServer;
using System.Configuration;

namespace Http2.Owin.WebApi.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            var address = ConfigurationManager.AppSettings["useSecurePort"] == "true" 
                                   ? "https://localhost:8443/"
                                   : "http://localhost:8080/";

            var startOpt = new StartOptions(address)
            {
                ServerFactory = typeof(SocketServerFactory).AssemblyQualifiedName,
            };
            

            try
            {
                // Start OWIN host 
                using (WebApp.Start<Startup>(startOpt))
                {
                    Console.WriteLine("Press Enter to stop the server");
                    
                    Console.ReadLine(); 
                } 
            }
            catch (Exception ex)
            {
                Console.WriteLine(String.Format("Error => {0} : {1}", 
                    ex.Message, 
                    (ex.InnerException != null) ? ex.InnerException.Message : String.Empty));
               	Console.ReadLine(); 
           
            }

            
        } 
    }
}
