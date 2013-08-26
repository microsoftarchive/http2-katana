using System;
using System.Configuration;
using System.IO;

namespace Client.Commands
{
    internal sealed class PostCommand : Command, IUriCommand
    {
        private Uri _uri;
        private readonly string _method;

        public Uri Uri
        {
            get { return _uri; }
        }

        public string Path { get { return _uri.PathAndQuery; } }
        public string Method { get { return _method; } }
        public string LocalPath { get; private set; }
        public string ServerPostAct { get; private set; }

        internal PostCommand()
        {
            _method = "post";
        }

        internal override void Parse(string[] cmdArgs)
        {
            if (cmdArgs.Length != 2 || Uri.TryCreate(cmdArgs[0], UriKind.Absolute, out _uri) == false
                || System.IO.Path.GetFileName(cmdArgs[0]) == String.Empty)
            {
                throw new InvalidOperationException("Invalid Post format!");
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
                throw new InvalidOperationException("Invalid scheme or port! Use https for secure port");
            }

            ServerPostAct = _uri.PathAndQuery;

            LocalPath = cmdArgs[1];

            if (!File.Exists(LocalPath))
            {
                throw new FileNotFoundException(String.Format("The file {0} doesn't exists!", LocalPath));
            }
        }

        internal override CommandType GetCmdType()
        {
            return CommandType.Post;
        }
    }
}
