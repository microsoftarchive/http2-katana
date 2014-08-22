// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.
using System;
using System.Configuration;
using System.IO;

namespace Http2.TestClient.Commands
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
                securePort = ClientOptions.SecurePort;
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
