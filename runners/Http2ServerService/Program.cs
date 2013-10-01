using System;
using System.IO;
using System.ServiceProcess;

namespace Microsoft.Http2.Owin.Server.Service
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            var servicesToRun = new ServiceBase[] 
            { 
                new Http2ServerService() 
            };
            ServiceBase.Run(servicesToRun);
        }
    }
}
