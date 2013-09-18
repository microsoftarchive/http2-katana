using System;

namespace Http2.TestClient.Commands
{
    internal interface IUriCommand
    {
        Uri Uri { get; }
        string Method { get; }
        string Path { get; }
    }
}
