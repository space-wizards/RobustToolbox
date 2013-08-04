using System;
using System.Reflection;
using System.Threading;
using SS13_Shared.ServerEnums;
using ServerServices.Log;

namespace SS13_Server
{
    internal class EntryPoint
    {
        private SS13Server _server;
        private Timer t;

        private static void Main(string[] args)
        {
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
    }
}