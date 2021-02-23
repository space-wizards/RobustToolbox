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

namespace Robust.Server
{

    internal static class Program
    {

        private static bool _hasStarted;

        internal static void Main(string[] args)
        {
            Start(args);
        }

        internal static void Start(string[] args, bool contentStart = false)
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

            ThreadPool.SetMinThreads(Environment.ProcessorCount * 2, Environment.ProcessorCount);

            // this sets up TaskScheduler.Current for new tasks to be scheduled off the main thread
            // the LongRunning option causes it to not be scheduled on the thread pool, so it's just a plain thread with a task context
            new TaskFactory(
                    CancellationToken.None,
                    TaskCreationOptions.LongRunning,
                    TaskContinuationOptions.None,
                    RobustTaskScheduler.Instance
                ).StartNew(() => ParsedMain(parsed, contentStart))
                .GetAwaiter().GetResult();
        }

        private static void ParsedMain(CommandLineArgs args, bool contentStart)
        {
            Thread.CurrentThread.Name = "Main Thread";
            IoCManager.InitThread();
            ServerIoC.RegisterIoC();
            IoCManager.BuildGraph();
            SetupLogging();
            InitReflectionManager();

            var server = IoCManager.Resolve<IBaseServerInternal>();

            // When the game is ran with the startup executable being content,
            // we have to disable the separate load context.
            // Otherwise the content assemblies will be loaded twice which causes *many* fun bugs.
            server.DisableLoadContext = contentStart;
            server.SetCommandLineArgs(args);

            Logger.Info("Server -> Starting");

            if (server.Start())
            {
                Logger.Fatal("Server -> Can not start server");
                //Not like you'd see this, haha. Perhaps later for logging.
                return;
            }

            string strVersion = Assembly.GetExecutingAssembly().GetName().Version!.ToString();
            Logger.Info("Server Version " + strVersion + " -> Ready");

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
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
#if WINDOWS_USE_UTF8_CONSOLE
                System.Console.OutputEncoding = Encoding.UTF8;
#else
                System.Console.OutputEncoding = Encoding.Unicode;
#endif
            }

            var mgr = IoCManager.Resolve<ILogManager>();
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
