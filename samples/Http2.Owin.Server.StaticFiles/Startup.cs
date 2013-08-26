using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Http2.Owin.Server.StaticFiles
{
    public class Startup
    {
        public void Configuration(IAppBuilder builder)
        {
            builder.UseHttp2();
            builder.UseStaticFiles("root");
        }
    }
}
