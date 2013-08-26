using System;

namespace Client.Commands
{
    internal interface IUriCommand
    {
        Uri Uri { get; }
        string Method { get; }
        string Path { get; }
    }
}
