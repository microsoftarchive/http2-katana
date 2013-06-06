using System;
using System.Configuration;
using Microsoft.Owin.Hosting;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            string connectString = null;

            if (args.Length == 0 || String.IsNullOrEmpty(args[0]) || args[0].ToLower() != "-secure")
            {
                connectString = ConfigurationManager.AppSettings["unsecureAddress"];
            }
            else
            {
                connectString = ConfigurationManager.AppSettings["secureAddress"];
            }

            using (WebApplication.Start<Startup>(options =>
                {
                    options.Url = connectString;
                    options.Server =
                        //"Microsoft.Owin.Host.HttpListener"; // No opaque or 2.0 frames
                        // "Microsoft.Owin.Host.HttpSys"; // Opaque only
                         "SocketServer"; // 2.0 frames only
                        // "Firefly"; // Opaque?
                }))
            {

            }
        }
    }
}
