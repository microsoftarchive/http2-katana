// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
namespace Http2.Owin.Service.Sample
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