using SS14.Server.Services.Log;

using SS14.Shared.ServerEnums;
using SS14.Shared.Utility;
using System;
using System.Reflection;
using System.Threading;

namespace SS14.Server
{
    internal class EntryPoint
    {
        private SS14Server _server;
        private Timer t;
        private static bool fullDump = false;

        private static void Main(string[] args)
        {
            //Process command-line args
            processArgs(args);
            //Register minidump dumper only if the app isn't being debugged. No use filling up hard drives with shite
      
    
            var main = new EntryPoint();
            main._server = new SS14Server();
            LogManager.Log("Server -> Starting");

            if (main._server.Start())
            {
                LogManager.Log("Server -> Can not start server", LogLevel.Fatal);
                //Not like you'd see this, haha. Perhaps later for logging.
                Environment.Exit(0);
            }

            string strVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            LogManager.Log("Server Version " + strVersion + " -> Ready");

            main._server.MainLoop();
        }

        private static void processArgs(string[] args)
        {
            for(var i = 0; i < args.Length;i++)
            {
                switch(args[i])
                {
                    case "--fulldump":
                        fullDump = true;
                        break;
                }
            }
        }
    }
}
