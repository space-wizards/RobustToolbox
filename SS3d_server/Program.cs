using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using SS13_Shared.Minidump;
using SS13_Shared.ServerEnums;
using ServerServices.Log;

namespace SS13_Server
{
    internal class EntryPoint
    {
        private SS13Server _server;
        private Timer t;
        private static bool fullDump = false;

        private static void Main(string[] args)
        {
            //Process command-line args
            processArgs(args);
            //Register minidump dumper only if the app isn't being debugged. No use filling up hard drives with shite
            if(!System.Diagnostics.Debugger.IsAttached)
                MiniDump.Register("crashdump-" + Guid.NewGuid().ToString("N") + ".dmp",
                                  fullDump
                                      ? MiniDump.MINIDUMP_TYPE.MiniDumpWithFullMemory
                                      : MiniDump.MINIDUMP_TYPE.MiniDumpNormal);
            var main = new EntryPoint();
            main._server = new SS13Server();
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