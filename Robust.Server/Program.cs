using System;
using System.Collections.Generic;
using System.Reflection;
using Robust.Server.Interfaces;
using Robust.Shared.ContentPack;
using Robust.Shared.Interfaces.Log;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared;

namespace Robust.Server
{
    internal static class Program
    {
        private static bool _hasStarted;

        internal static void Main(string[] args)
        {
            Start(args);
        }

        internal static void Start(string[] args)
        {
            if (_hasStarted)
            {
                throw new InvalidOperationException("Cannot start twice!");
            }

            _hasStarted = true;

            if (CommandLineArgs.TryParse(args, out var parsed))
            {
                ParsedMain(parsed);
            }
        }

        private static void ParsedMain(CommandLineArgs args)
        {
            IoCManager.InitThread();
            ServerIoC.RegisterIoC();
            IoCManager.BuildGraph();
            SetupLogging();
            InitReflectionManager();

            var server = IoCManager.Resolve<IBaseServerInternal>();

            server.SetCommandLineArgs(args);

            Logger.Info("Server -> Starting");

            if (server.Start())
            {
                Logger.Fatal("Server -> Can not start server");
                //Not like you'd see this, haha. Perhaps later for logging.
                return;
            }

            string strVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            Logger.Info("Server Version " + strVersion + " -> Ready");

            IoCManager.Resolve<ISignalHandler>().MaybeStart();

            server.MainLoop();

            Logger.Info("Goodbye.");

            // Used to dispose of systems that want to be disposed.
            // Such as the log manager.
            IoCManager.Clear();
        }

        internal static void InitReflectionManager()
        {
            // gets a handle to the shared and the current (server) dll.
            IoCManager.Resolve<IReflectionManager>().LoadAssemblies(new List<Assembly>(2)
            {
                AppDomain.CurrentDomain.GetAssemblyByName("Robust.Shared"),
                Assembly.GetExecutingAssembly()
            });
        }

        internal static void SetupLogging()
        {
            var mgr = IoCManager.Resolve<ILogManager>();
            var handler = new ConsoleLogHandler();
            mgr.RootSawmill.AddHandler(handler);
            mgr.GetSawmill("res.typecheck").Level = LogLevel.Info;
            mgr.GetSawmill("go.sys").Level = LogLevel.Info;
        }
    }
}
