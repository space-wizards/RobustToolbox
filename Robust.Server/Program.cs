using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Asynchronous;
using Robust.Shared.Reflection;
using Robust.Shared.Utility;

namespace Robust.Server
{

    internal static class Program
    {

        private static bool _hasStarted;

        internal static void Main(string[] args)
        {
            Start(args, new ServerOptions());
        }

        internal static void Start(string[] args, ServerOptions options, bool contentStart = false)
        {
            if (_hasStarted)
            {
                throw new InvalidOperationException("Cannot start twice!");
            }

            _hasStarted = true;

            if (!CommandLineArgs.TryParse(args, out var parsed))
            {
                return;
            }

            ParsedMain(parsed, contentStart, options);
        }

        private static void ParsedMain(CommandLineArgs args, bool contentStart, ServerOptions options)
        {
            ServerWarmup.RunWarmup();

            Thread.CurrentThread.Name = "Main Thread";
            var deps = IoCManager.InitThread();
            ServerIoC.RegisterIoC(deps);
            deps.BuildGraph();
            SetupLogging(deps);
            InitReflectionManager(deps);

            var server = deps.Resolve<IBaseServerInternal>();
            var logger = deps.Resolve<ILogManager>().RootSawmill;

            server.ContentStart = contentStart;
            server.SetCommandLineArgs(args);

            logger.Info("Server -> Starting");

            if (server.Start(options))
            {
                logger.Fatal("Server -> Can not start server");
                //Not like you'd see this, haha. Perhaps later for logging.
                return;
            }

            string strVersion = Assembly.GetExecutingAssembly().GetName().Version!.ToString();
            logger.Info("Server Version " + strVersion + " -> Ready");

            server.MainLoop();

            logger.Info("Goodbye.");

            // Used to dispose of systems that want to be disposed.
            // Such as the log manager.
            deps.Clear();
        }

        internal static void InitReflectionManager(IDependencyCollection deps)
        {
            // gets a handle to the shared and the current (server) dll.
            deps.Resolve<IReflectionManager>().LoadAssemblies(new List<Assembly>(2)
            {
                AppDomain.CurrentDomain.GetAssemblyByName("Robust.Shared"),
                Assembly.GetExecutingAssembly()
            });
        }

        internal static void SetupLogging(IDependencyCollection deps)
        {
            if (OperatingSystem.IsWindows())
            {
#if WINDOWS_USE_UTF8_CONSOLE
                System.Console.OutputEncoding = Encoding.UTF8;
#else
                System.Console.OutputEncoding = Encoding.Unicode;
#endif
            }

            var mgr = deps.Resolve<ILogManager>();
            var handler = new ConsoleLogHandler();
            mgr.RootSawmill.AddHandler(handler);
            mgr.GetSawmill("res.typecheck").Level = LogLevel.Info;
            mgr.GetSawmill("go.sys").Level = LogLevel.Info;
            mgr.GetSawmill("loc").Level = LogLevel.Error;
            // mgr.GetSawmill("szr").Level = LogLevel.Info;

#if DEBUG_ONLY_FCE_INFO
#if DEBUG_ONLY_FCE_LOG
            var fce = mgr.GetSawmill("fce");
#endif
            AppDomain.CurrentDomain.FirstChanceException += (sender, args) =>
            {
                // TODO: record FCE stats
#if DEBUG_ONLY_FCE_LOG
                fce.Fatal(message);
#endif
            }
#endif

            var uh = mgr.GetSawmill("unhandled");
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                var message = ((Exception) args.ExceptionObject).ToString();
                uh.Log(args.IsTerminating ? LogLevel.Fatal : LogLevel.Error, message);
            };

            var uo = mgr.GetSawmill("unobserved");
            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                uo.Error(args.Exception!.ToString());
#if EXCEPTION_TOLERANCE
                args.SetObserved(); // don't crash
#endif
            };
        }

    }

}
