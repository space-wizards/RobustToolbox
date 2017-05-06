using SS14.Server.Services.Log;
using SS14.Shared.ServerEnums;
using System;
using System.Reflection;


namespace SS14.Server
{
    internal class EntryPoint
    {
        private SS14Server _server;

        private static void Main(string[] args)
        {
            //Process command-line args
            var parsedArgs = processArgs(args);
            //Register minidump dumper only if the app isn't being debugged. No use filling up hard drives with shite


            var main = new EntryPoint();
            main._server = new SS14Server(parsedArgs);
            LogManager.Log("Server -> Starting");

            if (main._server.Start())
            {
                LogManager.Log("Server -> Can not start server", LogLevel.Fatal);
                //Not like you'd see this, haha. Perhaps later for logging.
                Environment.Exit(0);
            }

            string strVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            LogManager.Log("Server Version " + strVersion + " -> Ready");

            #if __MonoCS__
            SignalHander.InstallSignals();
            #endif

            main._server.MainLoop();
        }

        private static CommandLineArgs processArgs(string[] args)
        {
            var options = new CommandLineArgs();
            bool result = CommandLine.Parser.Default.ParseArguments(args, options);
            if (!result)
            {
                Environment.Exit(0);
            }

            return options;
        }
    }
}
