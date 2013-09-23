using System;

namespace Http2.TestClient.CommandParser
{
    internal static class HelpDisplayer
    {
        internal static void ShowMainMenuHelp()
        {
            Console.WriteLine("HTTP2 Prototype Client help\n");
            Console.WriteLine("HELP                                                     Display this help information");
            Console.WriteLine("HELP command                                             Display detailed help for command\n" +
                              "                                                         Ex. HELP GET");
            Console.WriteLine("GET <resource url>                                       Download resource from the specified url.\n" +
                              "                                                         E.g.: get https://localhost:8443/test.txt");
            Console.WriteLine("PUT <server url>/<alias> <local url>                     Put local resource on a server.\n" +
                              "                                                         E.g.: put https://localhost:8443/test.html D:\\README.txt");
            Console.WriteLine("POST <server url>/<server action> <local url>            Post local resource on a server and perform specified action then.\n" +
                              "                                                         E.g.: post https://localhost:8443/test.html D:\\README.txt");
            Console.WriteLine("DIR <server url>                                         Get files located in a server's root\n" +
                              "                                                         E.g.: dir https://localhost:8443");
            Console.WriteLine("DELETE <server url>/<filename>                           Send delete request to the server\n" +
                              "                                                         E.g.: delete https://localhost:8443/index.html");
#if HTTP11
            Console.WriteLine("HTTP11GET <filename>          Download file using HTTP 1.1.");
#endif
            Console.WriteLine("PING                                                     Pings opened connection\n" +
                              "                                                         E.g.: ping https://localhost:8443/");
            Console.WriteLine("EXIT                                                     Exit application");
            Console.WriteLine();
        }

        internal static void ShowExitCommandHelp()
        {
            Console.WriteLine("EXIT   Exit application\n");
            Console.WriteLine("  EXIT does not close current session.");
            Console.WriteLine("\n");
        }

        internal static void ShowHelpCommandHelp()
        {
            Console.WriteLine("HELP   Displays help\n");
            Console.WriteLine("  HELP without arguments displays list of command with short description.");
            Console.WriteLine("  HELP COMMAND displays detailed help for COMMAND.");
            Console.WriteLine("\n");
        }

        internal static void ShowGetCommandHelp()
        {
            Console.WriteLine("GET <resource url> Download web page and associated resources.\n");
            Console.WriteLine("  <resource url> should be path to web page.");
            Console.WriteLine("  Localy downloaded files are stored in directory relative to current.");
            Console.WriteLine("  Directory structure is preserved.");
            Console.WriteLine("  Download is done using HTTP2 protocol.");
            Console.WriteLine("  Examples of GET:\n");
            Console.WriteLine("  GET https://localhost:8443/test.txt");
            Console.WriteLine("\n");
        }

        internal static void ShowPutCommandHelp()
        {
            Console.WriteLine("PUT <server url> <local url>");
            Console.WriteLine(   "Upload local file to server.\n");
            Console.WriteLine("  <local url> should be local path to resource.");
            Console.WriteLine("  Upload is done using HTTP2 protocol.");
            Console.WriteLine("  Examples of PUT:\n");
            Console.WriteLine("  PUT https://localhost:8443/test.html  C:\test.txt");
            Console.WriteLine("\n");
        }

        internal static void ShowPostCommandHelp()
        {
            Console.WriteLine("POST <server url>/<server action> <local url>\n");
            Console.WriteLine("  Upload local file to server and\n");
            Console.WriteLine("  let server to perform specified action.\n");
            Console.WriteLine("  <local url> should be local path to resource.");
            Console.WriteLine("  <server action> is file name which will be used as handler for incoming data");
            Console.WriteLine("  This attribute is used only for saving file for now");
            Console.WriteLine("  Upload is done using HTTP2 protocol.");
            Console.WriteLine("  Examples of POST:\n");
            Console.WriteLine("  POST https://localhost:8443/test.html C:\test.txt view");
            Console.WriteLine("\n");
        }

        internal static void ShowDirCommandHelp()
        {
            Console.WriteLine("Dir <server url>");
            Console.WriteLine("  Get files located in a server's root\n");
            Console.WriteLine("  and save result to the index.html\n");
            Console.WriteLine("  located in the client's directory");
            Console.WriteLine("  Examples of Dir:\n");
            Console.WriteLine("  dir https://localhost:8443");
            Console.WriteLine("\n");
        }

        internal static void ShowDeleteCommandHelp()
        {
            Console.WriteLine("Delete <server url>/<filename>");
            Console.WriteLine("  Send delete request to the server\n");
            Console.WriteLine("  This command will always return AccessDenied webpage\n");
            Console.WriteLine("  Examples of Delete:\n");
            Console.WriteLine("  delete https://localhost:8443/index.html");
            Console.WriteLine("\n");
        }

        internal static void ShowPingCommandHelp()
        {
            Console.WriteLine("Pings the remote endpoint if you are connected to it");
            Console.WriteLine("ping https://localhost:8443/");
        }
    }
}
