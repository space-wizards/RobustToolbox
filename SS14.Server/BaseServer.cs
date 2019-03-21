using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using SS14.Server.Console;
using SS14.Server.GameStates;
using SS14.Server.Interfaces;
using SS14.Server.Interfaces.Chat;
using SS14.Server.Interfaces.Console;
using SS14.Server.Interfaces.GameObjects;
using SS14.Server.Interfaces.GameState;
using SS14.Server.Interfaces.Placement;
using SS14.Server.Interfaces.Player;
using SS14.Shared;
using SS14.Shared.Configuration;
using SS14.Shared.ContentPack;
using SS14.Shared.GameStates;
using SS14.Shared.Interfaces;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Interfaces.Serialization;
using SS14.Shared.Interfaces.Timing;
using SS14.Shared.Interfaces.Timers;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Network;
using SS14.Shared.Network.Messages;
using SS14.Shared.Prototypes;
using SS14.Shared.Map;
using SS14.Server.Interfaces.Maps;
using SS14.Server.Interfaces.ServerStatus;
using SS14.Server.Player;
using SS14.Server.ViewVariables;
using SS14.Shared.Asynchronous;
using SS14.Shared.Enums;
using SS14.Shared.Reflection;
using SS14.Shared.Timing;
using SS14.Shared.Utility;
using SS14.Shared.Interfaces.Log;
using SS14.Shared.Interfaces.Resources;
using SS14.Shared.Exceptions;

namespace SS14.Server
{
    /// <summary>
    /// The master class that runs the rest of the engine.
    /// </summary>
    public class BaseServer : IBaseServer
    {
        [Dependency]
        private readonly ICommandLineArgs _commandLine;
        [Dependency]
        private readonly IConfigurationManager _config;
        [Dependency]
        private readonly IComponentManager _components;
        [Dependency]
        private readonly IServerEntityManager _entities;
        [Dependency]
        private readonly ILogManager _log;
        [Dependency]
        private readonly ISS14Serializer _serializer;
        [Dependency]
        private readonly IGameTiming _time;
        [Dependency]
        private readonly IResourceManager _resources;
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

        private FileLogHandler fileLogHandler;
        private GameLoop _mainLoop;

        [Dependency]
        private IRuntimeLog runtimeLog;

        private TimeSpan _lastTitleUpdate;
        private int _lastReceivedBytes;
        private int _lastSentBytes;

        /// <inheritdoc />
        public int MaxPlayers => _config.GetCVar<int>("game.maxplayers");

        /// <inheritdoc />
        public string ServerName => _config.GetCVar<string>("game.hostname");

        /// <inheritdoc />
        public void Restart()
        {
            Logger.Info("[SRV] Restarting Server...");

            Cleanup();
            Start();
        }

        /// <inheritdoc />
        public void Shutdown(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                Logger.Info("[SRV] Shutting down...");
            else
                Logger.Info($"[SRV] {reason}, shutting down...");

            _mainLoop.Running = false;
            _log.RootSawmill.RemoveHandler(fileLogHandler);
            fileLogHandler.Dispose();
        }

        /// <inheritdoc />
        public bool Start()
        {

            //Sets up the configMgr
            _config.LoadFromFile(_commandLine.ConfigFile);

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

            // Set up the VFS
            _resources.Initialize();

#if RELEASE
            _resources.MountContentDirectory(@"./Resources/");
#else
            // Load from the resources dir in the repo root instead.
            // It's a debug build so this is fine.
            _resources.MountContentDirectory(@"../../Resources/");
            _resources.MountContentDirectory(@"../../../bin/Content.Server/", new ResourcePath("/Assemblies/"));
#endif

            //mount the engine content pack
            // _resources.MountContentPack(@"EngineContentPack.zip");

            //mount the default game ContentPack defined in config
            // _resources.MountDefaultContentPack();

            //identical code in game controller for client
            if (!AssemblyLoader.TryLoadAssembly<GameShared>(_resources, $"Content.Shared"))
            {
                Logger.Warning($"[ENG] Could not load any Shared DLL.");
            }

            if (!AssemblyLoader.TryLoadAssembly<GameServer>(_resources, $"Content.Server"))
            {
                Logger.Warning($"[ENG] Could not load any Server DLL.");
            }

            // HAS to happen after content gets loaded.
            // Else the content types won't be included.
            // TODO: solve this properly.
            _serializer.Initialize();

            // Initialize Tier 2 services
            _stateManager.Initialize();
            _entities.Initialize();
            IoCManager.Resolve<IChatManager>().Initialize();
            IoCManager.Resolve<IPlayerManager>().Initialize(MaxPlayers);
            _mapManager.Initialize();
            IoCManager.Resolve<IPlacementManager>().Initialize();
            IoCManager.Resolve<IViewVariablesHost>().Initialize();

            // Call Init in game assemblies.
            AssemblyLoader.BroadcastRunLevel(AssemblyLoader.RunLevel.Init);

            // because of 'reasons' this has to be called after the last assembly is loaded
            // otherwise the prototypes will be cleared
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            prototypeManager.LoadDirectory(new ResourcePath(@"/Prototypes"));
            prototypeManager.Resync();

            IoCManager.Resolve<ITileDefinitionManager>().Initialize();
            IoCManager.Resolve<IConsoleShell>().Initialize();
            IoCManager.Resolve<IConGroupController>().Initialize();

            AssemblyLoader.BroadcastRunLevel(AssemblyLoader.RunLevel.PostInit);

            _entities.Startup();
            IoCManager.Resolve<IStatusHost>().Start();

            return false;
        }

        /// <inheritdoc />
        public void MainLoop()
        {
            _mainLoop = new GameLoop(_time)
            {
                SleepMode = SleepMode.Delay
            };

            _mainLoop.Tick += (sender, args) => Update(args.DeltaSeconds);

            // set GameLoop.Running to false to return from this function.
            _mainLoop.Run();
            Cleanup();
        }

        /// <summary>
        ///     Updates the console window title with performance statistics.
        /// </summary>
        private void UpdateTitle()
        {
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

            _time.TickRate = _config.GetCVar<int>("net.tickrate");

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
            var pathToWrite = System.IO.Path.Combine(PathHelpers.ExecutableRelativeFile(logPath), "/Runtime-", DateTime.Now.ToShortDateString());
            System.IO.File.WriteAllText(pathToWrite, runtimeLog.Display());

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

        private void Update(float frameTime)
        {
            UpdateTitle();
            _systemConsole.Update();

            IoCManager.Resolve<IServerNetManager>().ProcessPackets();

            AssemblyLoader.BroadcastUpdate(AssemblyLoader.UpdateLevel.PreEngine, frameTime);

            timerManager.UpdateTimers(frameTime);
            _taskManager.ProcessPendingTasks();

            _components.CullRemovedComponents();
            _entities.Update(frameTime);

            AssemblyLoader.BroadcastUpdate(AssemblyLoader.UpdateLevel.PostEngine, frameTime);

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
