using System.ComponentModel;

namespace Microsoft.Http2.Owin.Server.Service
{
    [RunInstaller(true)]
    public partial class Http2ServerInstaller : System.Configuration.Install.Installer
    {
        public Http2ServerInstaller()
        {
            InitializeComponent();
        }
    }
}
