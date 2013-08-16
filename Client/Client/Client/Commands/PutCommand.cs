using System;
using System.Configuration;
using System.IO;
using Client.CommandParser;

namespace Client.Commands
{
    internal sealed class PutCommand : Command, IUriCommand
    {
        private Uri _uri;
        private readonly string _method;

        public Uri Uri
        {
            get { return _uri; }
        }

        public string Path { get { return _uri.PathAndQuery; } }
        public string LocalPath { get; private set; }
        public string Method { get { return _method; } }

        internal PutCommand()
        {
            _method = "put";
        }

        internal override void Parse(string[] cmdArgs)
        {
            //If port wasn't specified then it will be 80.
            if (cmdArgs.Length != 2 || Uri.TryCreate(cmdArgs[0], UriKind.Absolute, out _uri) == false
                || System.IO.Path.GetFileName(cmdArgs[0]) == String.Empty)
            {
                throw new InvalidOperationException("Invalid Put format!");
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

            LocalPath = cmdArgs[1];

            if (!File.Exists(LocalPath))
            {
                throw new FileNotFoundException(String.Format("The file {0} doesn't exists!", LocalPath));
            }
        }

        internal override CommandType GetCmdType()
        {
            return CommandType.Put;
        }
    }
}
