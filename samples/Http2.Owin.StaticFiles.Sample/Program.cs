using Microsoft.Owin.Hosting;
using SocketServer;
using System;

namespace Http2.Owin.StaticFiles.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            const string address = "https://localhost:8443/";
            // open https://localhost:8443/simpleTest.txt or https://localhost:8443/10mbTest.txt for example

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
                    Console.WriteLine("The following URLs could be used for testing:");
                    Console.WriteLine(address + "simpleTest.txt");
                    Console.WriteLine(address + "10mbTest.txt");
                    
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
