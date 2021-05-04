using System;
using System.Threading;
using Robust.LoaderApi;
using Robust.Shared;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Client
{
    internal partial class GameController
    {
        private IGameLoop? _mainLoop;

        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IDependencyCollection _dependencyCollection = default!;

        private static bool _hasStarted;

        private Thread? _gameThread;

        public static void Main(string[] args)
        {
            Start(args);
        }

        public static void Start(string[] args, bool contentStart = false, IMainArgs? loaderArgs=null, GameControllerOptions? options = null)
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

        private static void ParsedMain(CommandLineArgs args, bool contentStart, IMainArgs? loaderArgs, GameControllerOptions? options)
        {
            IoCManager.InitThread();

            var mode = args.Headless ? DisplayMode.Headless : DisplayMode.Clyde;

            InitIoC(mode);

            var gc = IoCManager.Resolve<GameController>();
            gc.SetCommandLineArgs(args);
            gc._loaderArgs = loaderArgs;
            if(options != null)
                gc.Options = options;

            // When the game is ran with the startup executable being content,
            // we have to disable the separate load context.
            // Otherwise the content assemblies will be loaded twice which causes *many* fun bugs.
            gc.ContentStart = contentStart;

            gc.Run(mode);
        }

        public void OverrideMainLoop(IGameLoop gameLoop)
        {
            _mainLoop = gameLoop;
        }

        public void Run(DisplayMode mode, Func<ILogHandler>? logHandlerFactory = null)
        {
            if (!StartupSystemSplash(logHandlerFactory))
            {
                Logger.Fatal("Failed to start game controller!");
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
                    Name = "Game thread",
                };

                _gameThread.Start();

                // Will block until game exit
                _clyde.EnterWindowLoop();

                if (_gameThread.IsAlive)
                {
                    Logger.Debug("Window loop exited; waiting for game thread to exit");
                    _gameThread.Join();
                }
            }
            else
            {
                ContinueStartupAndLoop(mode);
            }

            Cleanup();

            Logger.Debug("Goodbye");
            IoCManager.Clear();
        }

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
                Logger.Fatal("Failed to start game controller!");
                return;
            }

            DebugTools.AssertNotNull(_mainLoop);
            _mainLoop!.Run();
        }
    }
}
