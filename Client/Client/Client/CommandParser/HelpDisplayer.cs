using System;

namespace Client
{
    internal static class HelpDisplayer
    {
        internal static void ShowMainMenuHelp()
        {
            Console.WriteLine("HTTP2 Prototype Client help\n");
            Console.WriteLine("HELP                          Display this help information");
            Console.WriteLine("HELP command                  Display detailed help for command\n" +
                              "                              Ex. HELP GET");
            //Console.WriteLine("DIR <host url>                List files on server.");
            Console.WriteLine("GET <resource url>            Download web page and associated resources.\n" +
                              "                              E.g.: http://localhost:8443/test.txt");
            //Console.WriteLine("VERBOSE   [1|2|3]             Display verbose output.");
            //Console.WriteLine("CAPTURE-STATS [On|Off|Reset]  Start/stop/reset protocol monitoring.");
           // Console.WriteLine("DUMP-STATS                    Display statistics captured using CAPTURE-STATS.");
#if HTTP11
            Console.WriteLine("HTTP11GET <filename>          Download file using HTTP 1.1.");
#endif
            //Console.WriteLine("RUN  <filename>               Run command script");
            Console.WriteLine("EXIT                          Exit application");
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
            Console.WriteLine("  GET http://localhost:8443/test.txt");
            Console.WriteLine("\n");
        }
    }
}
