using System;
using System.Configuration;

namespace Client
{
    internal sealed class GetCommand : Command
    {
        private Uri _uri;

        public Uri Uri
        {
            get { return _uri; }
        }
        
        internal GetCommand(string cmdBody)
        {
            Parse(cmdBody);
        }

        protected override void Parse(string cmd)
        {
            if (Uri.TryCreate(cmd, UriKind.Absolute, out _uri) == false)
            {
                throw new InvalidOperationException("Invalid Get command!");
            }

            int securePort;
            try
            {
                securePort = int.Parse(ConfigurationManager.AppSettings["securePort"]);
            }
            catch (Exception)
            {
                throw new InvalidOperationException("Invalid port in the config file");
            }

            if (Uri.Port == securePort
                && 
                Uri.Scheme == Uri.UriSchemeHttp
                ||
                Uri.Port != securePort
                && 
                Uri.Scheme == Uri.UriSchemeHttps)
            {
                throw new InvalidOperationException("Invalid scheme on port! Use https for secure port");
            }
        }

        internal override CommandType GetCmdType()
        {
            return CommandType.Get;
        }
    }
}
