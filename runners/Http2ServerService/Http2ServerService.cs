using System;
using System.Threading;
using Microsoft.Owin.Hosting;
using System.ServiceProcess;
using System.Configuration;

namespace Microsoft.Http2.Owin.Server.Service
{
    /// <summary>
    /// This class represents service start and stop logic
    /// </summary>
    public partial class Http2ServerService : ServiceBase
    {
        private Thread _http2ServerThread;
        private IDisposable _owinServer;

        public Http2ServerService()
        {
            InitializeComponent();
        }

        private void StartServer(string connectString)
        {

            var startOpt = new StartOptions(connectString)
            {
                ServerFactory = typeof(SocketServerFactory).AssemblyQualifiedName,
            };

            // Start OWIN host 
            _owinServer = WebApp.Start<Startup>(startOpt);
        }

        protected override void OnStart(string[] args)
        {
            bool isSecure = ConfigurationManager.AppSettings["useSecurePort"] == "true";
            string connectString = isSecure
                                       ? ConfigurationManager.AppSettings["secureAddress"]
                                       : ConfigurationManager.AppSettings["unsecureAddress"];

            _http2ServerThread = new Thread(() => StartServer(connectString));
            _http2ServerThread.Start();
        }

        protected override void OnStop()
        {
            if (_owinServer != null)
            {
                _owinServer.Dispose();
                _owinServer = null;
            }

            if (_http2ServerThread.IsAlive)
            {
                _http2ServerThread.Abort();
            }
        }
    }
}
