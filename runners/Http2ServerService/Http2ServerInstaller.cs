using System.ComponentModel;

namespace Microsoft.Http2.Owin.Server.Service
{
    /// <summary>
    /// This class is used for service installing
    /// </summary>
    [RunInstaller(true)]
    public partial class Http2ServerInstaller : System.Configuration.Install.Installer
    {
        public Http2ServerInstaller()
        {
            InitializeComponent();
        }
    }
}
