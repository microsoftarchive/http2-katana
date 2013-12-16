// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
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

            StartServer(connectString);
        }

        protected override void OnStop()
        {
            if (_owinServer != null)
            {
                _owinServer.Dispose();
                _owinServer = null;
            }
        }
    }
}
