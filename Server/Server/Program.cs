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
