using System.Threading;
using Microsoft.Owin.Hosting;
using System.ServiceProcess;
using System.Configuration;

namespace Http2ServerService
{
    public partial class Http2ServerService : ServiceBase
    {
        private Thread _http2ServerThread;

        public Http2ServerService()
        {
            InitializeComponent();
        }

        private void StartServer(string connectString)
        {
            using (WebApplication.Start<Startup>(options =>
            {
                options.Url = connectString;
                options.Server = "SocketServer";
            }))
            {

            }
        }

        protected override void OnStart(string[] args)
        {
            bool isSecure = ConfigurationManager.AppSettings["useSecurePort"] == "true";
            string connectString = isSecure
                                       ? ConfigurationManager.AppSettings["secureAddress"]
                                       : ConfigurationManager.AppSettings["unsecureAddress"];

            _http2ServerThread = new Thread((ThreadStart) delegate
                {
                    StartServer(connectString);
                });
            _http2ServerThread.Start();
        }

        protected override void OnStop()
        {
            if (_http2ServerThread.IsAlive)
            {
                _http2ServerThread.Abort();
            }
        }
    }
}
