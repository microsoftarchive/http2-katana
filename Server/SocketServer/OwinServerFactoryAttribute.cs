using Owin.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[assembly: SocketServer.OwinServerFactoryAttribute]

namespace SocketServer
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    [AttributeUsage(AttributeTargets.Assembly)]
    public class OwinServerFactoryAttribute : Attribute
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
            return new Http2SocketServer(app, properties);
        }
    }
}
