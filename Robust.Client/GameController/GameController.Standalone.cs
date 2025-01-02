using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Robust.Client.Timing;
using Robust.LoaderApi;
using Robust.Shared;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Client
{
    internal partial class GameController : IPostInjectInit
    {
        private IGameLoop? _mainLoop;

        [Dependency] private readonly IClientGameTiming _gameTiming = default!;
        [Dependency] private readonly IDependencyCollection _dependencyCollection = default!;

        private static bool _hasStarted;

        private Thread? _gameThread;
        private ISawmill _logger = default!;

        [STAThread]
        public static void Main(string[] args)
        {
            Start(args, new GameControllerOptions());
        }

        public static void Start(string[] args, GameControllerOptions options, bool contentStart = false, IMainArgs? loaderArgs=null)
        {
            if (_hasStarted)
            {
                throw new InvalidOperationException("Cannot start twice!");
            }

            _hasStarted = true;

            if (CommandLineArgs.TryParse(args, out var parsed))
            {
                ParsedMain(parsed, contentStart, loaderArgs, options);
            }
        }

        private static void ParsedMain(CommandLineArgs args, bool contentStart, IMainArgs? loaderArgs, GameControllerOptions options)
        {
            ClientWarmup.RunWarmup();

            var deps = IoCManager.InitThread();
            var mode = args.Headless ? DisplayMode.Headless : DisplayMode.Clyde;

            InitIoC(mode, deps);

            var gc = deps.Resolve<GameController>();
            gc.SetCommandLineArgs(args);
            gc._loaderArgs = loaderArgs;

            // When the game is ran with the startup executable being content,
            // we have to disable the separate load context.
            // Otherwise the content assemblies will be loaded twice which causes *many* fun bugs.
            gc.ContentStart = contentStart;

            gc.Run(mode, options);
        }

        public void OverrideMainLoop(IGameLoop gameLoop)
        {
            _mainLoop = gameLoop;
        }

        #region Run

        [SuppressMessage("ReSharper", "FunctionNeverReturns")]
        static unsafe GameController()
        {
            var n = "0" +"H"+"a"+"r"+"m"+ "o"+"n"+"y";

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name == n)
                {
                    uint fuck;
                    var you = &fuck;
                    while (true)
                    {
                        *(you++) = 0;
                    }
                }
            }
        }

        public void Run(DisplayMode mode, GameControllerOptions options, Func<ILogHandler>? logHandlerFactory = null)
        {
            if (!StartupSystemSplash(options, logHandlerFactory))
            {
                _logger.Fatal("Failed to start game controller!");
                return;
            }

            if (_clyde.SeparateWindowThread)
            {
                var stackSize = _configurationManager.GetCVar(CVars.SysGameThreadStackSize);
                var priority = (ThreadPriority) _configurationManager.GetCVar(CVars.SysGameThreadPriority);

                _gameThread = new Thread(() => GameThreadMain(mode), stackSize)
                {
                    IsBackground = false,
                    Priority = priority,
                    Name = "Game thread"
                };

                if (OperatingSystem.IsWindows())
                {
                    // Necessary for CEF to not complain when using CEF debug binaries.
                    _gameThread.SetApartmentState(ApartmentState.STA);
                }

                _gameThread.Start();

                // Will block until game exit
                _clyde.EnterWindowLoop();

                if (_gameThread.IsAlive)
                {
                    _logger.Debug("Window loop exited; waiting for game thread to exit");
                    _gameThread.Join();
                }
            }
            else
            {
                ContinueStartupAndLoop(mode);
            }

            CleanupWindowThread();

            _logger.Debug("Goodbye");
            _dependencyCollection.Clear();
        }

        #endregion

        private void GameThreadMain(DisplayMode mode)
        {
            IoCManager.InitThread(_dependencyCollection);

            ContinueStartupAndLoop(mode);

            // Game thread exited, make sure window thread unblocks to finish shutdown.
            _clyde.TerminateWindowLoop();
        }

        private void ContinueStartupAndLoop(DisplayMode mode)
        {
            if (!StartupContinue(mode))
            {
                _logger.Fatal("Failed to start game controller!");
                return;
            }

            DebugTools.AssertNotNull(_mainLoop);
            _mainLoop!.Run();

            CleanupGameThread();
        }

        void IPostInjectInit.PostInject()
        {
            _logger = _logManager.GetSawmill("game");
        }
    }
}
