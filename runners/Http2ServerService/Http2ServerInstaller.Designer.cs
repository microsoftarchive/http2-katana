namespace Microsoft.Http2.Owin.Server.Service
{
    partial class Http2ServerInstaller
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.serviceHttp2ServerProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.serviceHttp2ServerInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // serviceHttp2ServerProcessInstaller
            // 
            this.serviceHttp2ServerProcessInstaller.Password = null;
            this.serviceHttp2ServerProcessInstaller.Username = null;
            // 
            // serviceHttp2ServerInstaller
            // 
            this.serviceHttp2ServerInstaller.DisplayName = "Http2Server";
            this.serviceHttp2ServerInstaller.ServiceName = "Http2Server";
            this.serviceHttp2ServerInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.serviceHttp2ServerProcessInstaller,
            this.serviceHttp2ServerInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller serviceHttp2ServerProcessInstaller;
        private System.ServiceProcess.ServiceInstaller serviceHttp2ServerInstaller;
    }
}