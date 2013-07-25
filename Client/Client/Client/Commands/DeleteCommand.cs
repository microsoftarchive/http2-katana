using System;
using System.Configuration;

namespace Client.Commands
{
    internal class DeleteCommand : Command, IUriCommand
    {
        private Uri _uri;
        private readonly string _method;

        public Uri Uri
        {
            get { return _uri; }
        }

        public string Path { get { return _uri.PathAndQuery; } }
        public string Method { get { return _method; } }

        internal DeleteCommand()
        {
            _method = "delete";
        }

        internal override void Parse(string[] cmdArgs)
        {
            if (cmdArgs.Length != 1 || Uri.TryCreate(cmdArgs[0], UriKind.Absolute, out _uri) == false)
            {
                throw new InvalidOperationException("Invalid Delete format!");
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
            return CommandType.Delete;
        }
    }
}
