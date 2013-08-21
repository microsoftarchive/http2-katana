using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;

//[assembly: SocketServer.OwinServerFactoryAttribute]

namespace SocketServer
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    /// <summary>
    /// This class is used for specifying server properties and creating Http2SocketServer
    /// </summary>
    public class SocketServerFactory
    {
        public static void Initialize(IDictionary<string, object> properties)
        {
            if (properties == null)
            {
                throw new ArgumentNullException("properties");
            }
        }

        public static IDisposable Create(AppFunc app, IDictionary<string, object> properties)
        {
            bool useHandshake = ConfigurationManager.AppSettings["handshakeOptions"] != "no-handshake";
            bool usePriorities = ConfigurationManager.AppSettings["prioritiesOptions"] != "no-priorities";
            bool useFlowControl = ConfigurationManager.AppSettings["flowcontrolOptions"] != "no-flowcontrol";

            properties.Add("use-handshake", useHandshake);
            properties.Add("use-priorities", usePriorities);
            properties.Add("use-flowControl", useFlowControl);

            return new HttpSocketServer(app, properties);
        }
    }
}
