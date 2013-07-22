using System.ServiceProcess;


namespace Http2ServerService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[] 
            { 
                new Http2ServerService() 
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
