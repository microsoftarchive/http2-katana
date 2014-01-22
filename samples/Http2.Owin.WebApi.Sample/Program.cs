// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
using System;
using Microsoft.Http2.Owin.Server;
using Microsoft.Owin.Hosting;
using System.Configuration;

namespace Http2.Owin.WebApi.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            var address = ConfigurationManager.AppSettings["useSecurePort"] == "true" 
                                   ? "https://localhost:8443/"
                                   : "http://localhost:8080/";

            var startOpt = new StartOptions(address)
            {
                ServerFactory = typeof(SocketServerFactory).AssemblyQualifiedName,
            };
            

            try
            {
                // Start OWIN host 
                using (WebApp.Start<Startup>(startOpt))
                {
                    Console.WriteLine("Press Enter to stop the server");
                    
                    Console.ReadLine(); 
                } 
            }
            catch (Exception ex)
            {
                Console.WriteLine(String.Format("Error => {0} : {1}", 
                    ex.Message, 
                    (ex.InnerException != null) ? ex.InnerException.Message : String.Empty));
               	Console.ReadLine(); 
           
            }
        } 
    }
}
