using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Robust.Server.Console;
using Robust.Server.Interfaces;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.GameObjects;
using Robust.Server.Interfaces.GameState;
using Robust.Server.Interfaces.Placement;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.Interfaces.Timers;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Server.Interfaces.ServerStatus;
using Robust.Server.ViewVariables;
using Robust.Shared.Asynchronous;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.Interfaces.Log;
using Robust.Shared.Interfaces.Resources;
using Robust.Shared.Exceptions;
using Robust.Shared.Localization;
using Robust.Server.Interfaces.Debugging;

namespace Robust.Server
{
    /// <summary>
    /// The master class that runs the rest of the engine.
    /// </summary>
    internal sealed class BaseServer : IBaseServerInternal
    {
#pragma warning disable 649
        [Dependency]
        private readonly IConfigurationManager _config;
        [Dependency]
        private readonly IComponentManager _components;
        [Dependency]
        private readonly IServerEntityManager _entities;
        [Dependency]
        private readonly ILogManager _log;
        [Dependency]
        private readonly IRobustSerializer _serializer;
        [Dependency]
        private readonly IGameTiming _time;
        [Dependency]
        private readonly IResourceManagerInternal _resources;
        [Dependency]
        private readonly IMapManager _mapManager;
        [Dependency]
        private readonly ITimerManager timerManager;
        [Dependency]
        private readonly IServerGameStateManager _stateManager;
        [Dependency]
        private readonly IServerNetManager _network;
        [Dependency]
        private readonly ISystemConsoleManager _systemConsole;
        [Dependency]
        private readonly ITaskManager _taskManager;
        [Dependency]
        private readonly ILocalizationManager _localizationManager;
        [Dependency]
        private IRuntimeLog runtimeLog;
        [Dependency]
        private readonly IModLoader _modLoader;
#pragma warning restore 649

        private CommandLineArgs _commandLineArgs;
        private FileLogHandler fileLogHandler;
        private IGameLoop _mainLoop;

        private TimeSpan _lastTitleUpdate;
        private int _lastReceivedBytes;
        private int _lastSentBytes;

        public string ContentRootDir { get; set; } = "../../../";

        /// <inheritdoc />
        public int MaxPlayers => _config.GetCVar<int>("game.maxplayers");

        /// <inheritdoc />
        public string ServerName => _config.GetCVar<string>("game.hostname");

        /// <inheritdoc />
        public void Restart()
        {
            Logger.InfoS("srv", "Restarting Server...");

            Cleanup();
            Start();
        }

        /// <inheritdoc />
        public void Shutdown(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                Logger.InfoS("srv", "Shutting down...");
            else
                Logger.InfoS("srv", $"{reason}, shutting down...");

            _mainLoop.Running = false;
            _log.RootSawmill.RemoveHandler(fileLogHandler);
            fileLogHandler.Dispose();
        }

        public void SetCommandLineArgs(CommandLineArgs args)
        {
            _commandLineArgs = args;
        }

        /// <inheritdoc />
        public bool Start()
        {
            // Sets up the configMgr
            // If a config file path was passed, use it literally.
            // This ensures it's working-directory relative
            // (for people passing config file through the terminal or something).
            // Otherwise use the one next to the executable.
            if (_commandLineArgs?.ConfigFile != null)
            {
                _config.LoadFromFile(_commandLineArgs.ConfigFile);
            }
            else
            {
                var path = PathHelpers.ExecutableRelativeFile("server_config.toml");
                if (File.Exists(path))
                {
                    _config.LoadFromFile(path);
                }
                else
                {
                    _config.SetSaveFile(path);
                }
            }

            if (_commandLineArgs != null)
            {
                _config.OverrideConVars(_commandLineArgs.CVars);
            }


            //Sets up Logging
            _config.RegisterCVar("log.path", "logs", CVar.ARCHIVE);
            _config.RegisterCVar("log.format", "log_%(date)s-%(time)s.txt", CVar.ARCHIVE);
            _config.RegisterCVar("log.level", LogLevel.Info, CVar.ARCHIVE);

            var logPath = _config.GetCVar<string>("log.path");
            var logFormat = _config.GetCVar<string>("log.format");
            var logFilename = logFormat.Replace("%(date)s", DateTime.Now.ToString("yyyyMMdd")).Replace("%(time)s", DateTime.Now.ToString("hhmmss"));
            var fullPath = Path.Combine(logPath, logFilename);

            if (!Path.IsPathRooted(fullPath))
            {
                logPath = PathHelpers.ExecutableRelativeFile(fullPath);
            }

            fileLogHandler = new FileLogHandler(logPath);
            _log.RootSawmill.Level = _config.GetCVar<LogLevel>("log.level");
            _log.RootSawmill.AddHandler(fileLogHandler);

            // Has to be done early because this guy's in charge of the main thread Synchronization Context.
            _taskManager.Initialize();

            LoadSettings();

            var netMan = IoCManager.Resolve<IServerNetManager>();
            try
            {
                netMan.Initialize(true);
                netMan.StartServer();
            }
            catch (Exception e)
            {
                var port = netMan.Port;
                Logger.Fatal("Unable to setup networking manager. Check port {0} is not already in use and that all binding addresses are correct!\n{1}", port, e);
                return true;
            }

            var dataDir = _commandLineArgs?.DataDir ?? PathHelpers.ExecutableRelativeFile("data");

            // Set up the VFS
            _resources.Initialize(dataDir);

#if FULL_RELEASE
            _resources.MountContentDirectory(@"./Resources/");
#else
            // Load from the resources dir in the repo root instead.
            // It's a debug build so this is fine.
            _resources.MountContentDirectory($@"{ContentRootDir}RobustToolbox/Resources/");
            _resources.MountContentDirectory($@"{ContentRootDir}bin/Content.Server/", new ResourcePath("/Assemblies/"));
            _resources.MountContentDirectory($@"{ContentRootDir}Resources/");
#endif

            // Default to en-US.
            // Perhaps in the future we could make a command line arg or something to change this default.
            _localizationManager.LoadCulture(new CultureInfo("en-US"));


            //mount the engine content pack
            // _resources.MountContentPack(@"EngineContentPack.zip");

            //mount the default game ContentPack defined in config
            // _resources.MountDefaultContentPack();

            //identical code in game controller for client
            if (!_modLoader.TryLoadAssembly<GameShared>(_resources, $"Content.Shared"))
            {
                Logger.FatalS("eng", "Could not load any Shared DLL.");
                return true;
            }

            if (!_modLoader.TryLoadAssembly<GameServer>(_resources, $"Content.Server"))
            {
                Logger.FatalS("eng", "Could not load any Server DLL.");
                return true;
            }

            // HAS to happen after content gets loaded.
            // Else the content types won't be included.
            // TODO: solve this properly.
            _serializer.Initialize();

            // Initialize Tier 2 services
            IoCManager.Resolve<IGameTiming>().InSimulation = true;

            _stateManager.Initialize();
            _entities.Initialize();
            IoCManager.Resolve<IPlayerManager>().Initialize(MaxPlayers);
            _mapManager.Initialize();
            _mapManager.Startup();
            IoCManager.Resolve<IPlacementManager>().Initialize();
            IoCManager.Resolve<IViewVariablesHost>().Initialize();
            IoCManager.Resolve<IDebugDrawingManager>().Initialize();

            // Call Init in game assemblies.
            _modLoader.BroadcastRunLevel(ModRunLevel.Init);

            // because of 'reasons' this has to be called after the last assembly is loaded
            // otherwise the prototypes will be cleared
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            prototypeManager.LoadDirectory(new ResourcePath(@"/Prototypes"));
            prototypeManager.Resync();

            IoCManager.Resolve<IConsoleShell>().Initialize();
            IoCManager.Resolve<IConGroupController>().Initialize();
            _entities.Startup();

            _modLoader.BroadcastRunLevel(ModRunLevel.PostInit);

            IoCManager.Resolve<IStatusHost>().Start();

            return false;
        }

        /// <inheritdoc />
        public void MainLoop()
        {
            if (_mainLoop == null)
            {
                _mainLoop = new GameLoop(_time)
                {
                    SleepMode = SleepMode.Delay,
                    DetectSoftLock = true
                };
            }

            _mainLoop.Tick += (sender, args) => Update(args);

            // set GameLoop.Running to false to return from this function.
            _mainLoop.Run();

            _time.InSimulation = true;
            Cleanup();
        }

        public void OverrideMainLoop(IGameLoop gameLoop)
        {
            _mainLoop = gameLoop;
        }

        /// <summary>
        ///     Updates the console window title with performance statistics.
        /// </summary>
        private void UpdateTitle()
        {
            if (!Environment.UserInteractive || System.Console.IsInputRedirected)
            {
                return;
            }

            // every 1 second update stats in the console window title
            if ((_time.RealTime - _lastTitleUpdate).TotalSeconds < 1.0)
                return;

            var netStats = UpdateBps();
            System.Console.Title = string.Format("FPS: {0:N2} SD: {1:N2}ms | Net: ({2}) | Memory: {3:N0} KiB",
                Math.Round(_time.FramesPerSecondAvg, 2),
                _time.RealFrameTimeStdDev.TotalMilliseconds,
                netStats,
                Process.GetCurrentProcess().PrivateMemorySize64 >> 10);
            _lastTitleUpdate = _time.RealTime;
        }

        /// <summary>
        ///     Loads the server settings from the ConfigurationManager.
        /// </summary>
        private void LoadSettings()
        {
            var cfgMgr = IoCManager.Resolve<IConfigurationManager>();

            cfgMgr.RegisterCVar("net.tickrate", 60, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);

            cfgMgr.RegisterCVar("game.hostname", "MyServer", CVar.ARCHIVE);
            cfgMgr.RegisterCVar("game.maxplayers", 32, CVar.ARCHIVE);
            cfgMgr.RegisterCVar("game.type", GameType.Game);

            _time.TickRate = (byte)_config.GetCVar<int>("net.tickrate");

            Logger.InfoS("srv", $"Name: {ServerName}");
            Logger.InfoS("srv", $"TickRate: {_time.TickRate}({_time.TickPeriod.TotalMilliseconds:0.00}ms)");
            Logger.InfoS("srv", $"Max players: {MaxPlayers}");
        }

        // called right before main loop returns, do all saving/cleanup in here
        private void Cleanup()
        {
            // shut down networking, kicking all players.
            _network.Shutdown("Server Shutdown");

            // shutdown entities
            _entities.Shutdown();

            // Wrtie down exception log
            var logPath = _config.GetCVar<string>("log.path");
            var pathToWrite = Path.Combine(PathHelpers.ExecutableRelativeFile(logPath), "Runtime-" + DateTime.Now.ToString("yyyy-MM-dd-THH-mm-ss") + ".txt");
            File.WriteAllText(pathToWrite, runtimeLog.Display(), EncodingHelpers.UTF8);

            //TODO: This should prob shutdown all managers in a loop.
        }

        private string UpdateBps()
        {
            var stats = IoCManager.Resolve<IServerNetManager>().Statistics;

            var bps = $"Send: {(stats.SentBytes - _lastSentBytes) >> 10:N0} KiB/s, Recv: {(stats.ReceivedBytes - _lastReceivedBytes) >> 10:N0} KiB/s";

            _lastSentBytes = stats.SentBytes;
            _lastReceivedBytes = stats.ReceivedBytes;

            return bps;
        }

        private void Update(FrameEventArgs frameEventArgs)
        {
            UpdateTitle();
            _systemConsole.Update();

            IoCManager.Resolve<IServerNetManager>().ProcessPackets();

            _modLoader.BroadcastUpdate(ModUpdateLevel.PreEngine, frameEventArgs);

            timerManager.UpdateTimers(frameEventArgs);
            _taskManager.ProcessPendingTasks();

            _components.CullRemovedComponents();
            _entities.Update(frameEventArgs.DeltaSeconds);

            _modLoader.BroadcastUpdate(ModUpdateLevel.PostEngine, frameEventArgs);

            _stateManager.SendGameStateUpdate();
        }
    }

    /// <summary>
    ///     Type of game currently running.
    /// </summary>
    public enum GameType
    {
        MapEditor = 0,
        Game,
    }
}
