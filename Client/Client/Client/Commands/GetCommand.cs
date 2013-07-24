using System;
using System.Configuration;

namespace Client.Commands
{
    internal sealed class GetCommand : Command, IUriCommand
    {
        private Uri _uri;
        private readonly string _method;

        public Uri Uri
        {
            get { return _uri; }
        }

        public string Path { get { return _uri.PathAndQuery; } }
        public string Method { get { return _method; } }

        internal GetCommand(string[] cmdArgs)
        {
            _method = "get";
            Parse(cmdArgs);
        }

        protected override void Parse(string[] cmdArgs)
        {
            //If port wasn't specified then it will be 80.
            if (cmdArgs.Length != 1 || Uri.TryCreate(cmdArgs[0], UriKind.Absolute, out _uri) == false)
            {
                throw new InvalidOperationException("Invalid Get format!");
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
