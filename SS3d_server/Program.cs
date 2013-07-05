using System;
using System.Diagnostics;
using System.Threading;
using ServerServices.Log;
using SS13_Shared.ServerEnums;

namespace SS13_Server
{
    class EntryPoint
    {
        private SS13Server _server;
        private Timer t;

        static void Main(string[] args)
        {
            EntryPoint main = new EntryPoint();
            main._server = new SS13Server();
            LogManager.Log("Server -> Starting");

            if (main._server.Start())
            {
                LogManager.Log("Server -> Can not start server", LogLevel.Fatal); //Not like you'd see this, haha. Perhaps later for logging.
                Environment.Exit(0);
            }

            string strVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            LogManager.Log("Server Version " + strVersion + " -> Ready");

            main._server.MainLoop();
        }
    }
}
