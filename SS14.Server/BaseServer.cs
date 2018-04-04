using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using SS14.Server.GameStates;
using SS14.Server.Interfaces;
using SS14.Server.Interfaces.Chat;
using SS14.Server.Interfaces.ClientConsoleHost;
using SS14.Server.Interfaces.GameObjects;
using SS14.Server.Interfaces.GameState;
using SS14.Server.Interfaces.Log;
using SS14.Server.Interfaces.Placement;
using SS14.Server.Interfaces.Player;
using SS14.Server.Interfaces.ServerConsole;
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
using SS14.Server.Player;
using SS14.Shared.Enums;
using SS14.Shared.Reflection;
using SS14.Shared.Timing;

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
        private readonly IServerLogManager _log;
        [Dependency]
        private readonly ISS14Serializer _serializer;
        [Dependency]
        private readonly IGameTiming _time;
        [Dependency]
        private readonly IResourceManager _resources;
        [Dependency]
        private readonly IMapLoader _mapLoader;
        [Dependency]
        private readonly IMapManager _mapManager;
        [Dependency]
        private readonly ITimerManager timerManager;
        [Dependency]
        private readonly IGameStateManager _stateManager;
        [Dependency]
        private readonly IServerNetManager _network;

        private GameLoop _mainLoop;
        private ServerRunLevel _runLevel;

        private TimeSpan _lastTitleUpdate;
        private int _lastReceivedBytes;
        private int _lastSentBytes;

        /// <inheritdoc />
        public ServerRunLevel RunLevel
        {
            get => _runLevel;
            set => OnRunLevelChanged(value);
        }

        /// <inheritdoc />
        public string MapName => _config.GetCVar<string>("game.mapname");

        /// <inheritdoc />
        public int MaxPlayers => _config.GetCVar<int>("game.maxplayers");

        /// <inheritdoc />
        public string ServerName => _config.GetCVar<string>("game.hostname");

        /// <inheritdoc />
        public string Motd => _config.GetCVar<string>("game.welcomemsg");

        /// <inheritdoc />
        public string GameModeName { get; set; } = string.Empty;

        /// <inheritdoc />
        public event EventHandler<RunLevelChangedEventArgs> RunLevelChanged;

        /// <inheritdoc />
        public void Restart()
        {
            Logger.Info("[SRV] Restarting Server...");

            Cleanup();
            Start();
        }

        /// <inheritdoc />
        public void Shutdown(string reason = null)
        {
            if (string.IsNullOrWhiteSpace(reason))
                Logger.Log("[SRV] Shutting down...");
            else
                Logger.Log($"[SRV] {reason}, shutting down...");

            _mainLoop.Running = false;
        }

        /// <inheritdoc />
        public bool Start()
        {
            //Sets up the configMgr
            _config.LoadFromFile(_commandLine.ConfigFile);

            //Sets up Logging
            _config.RegisterCVar("log.path", "logs", CVar.ARCHIVE);
            _config.RegisterCVar("log.format", "log_%(date)s-%(time)s.txt", CVar.ARCHIVE);
            _config.RegisterCVar("log.level", LogLevel.Information, CVar.ARCHIVE);

            var logPath = _config.GetCVar<string>("log.path");
            var logFormat = _config.GetCVar<string>("log.format");
            var logFilename = logFormat.Replace("%(date)s", DateTime.Now.ToString("yyyyMMdd")).Replace("%(time)s", DateTime.Now.ToString("hhmmss"));
            var fullPath = Path.Combine(logPath, logFilename);

            if (!Path.IsPathRooted(fullPath))
                logPath = PathHelpers.ExecutableRelativeFile(fullPath);

            // Create log directory if it does not exist yet.
            Directory.CreateDirectory(Path.GetDirectoryName(logPath));

            _log.CurrentLevel = _config.GetCVar<LogLevel>("log.level");
            _log.LogPath = logPath;

            OnRunLevelChanged(ServerRunLevel.Init);

            LoadSettings();

            var netMan = IoCManager.Resolve<IServerNetManager>();
            try
            {
                netMan.Initialize(true);
                netMan.Startup();
            }
            catch (System.Net.Sockets.SocketException)
            {
                var port = netMan.Port;
                Logger.Log($"Unable to setup networking manager. Check port {port} is not already in use!, shutting down...", LogLevel.Fatal);
                Environment.Exit(1);
            }
            catch (Exception e)
            {
                Logger.Log($"Unable to setup networking manager. Unknown exception: {e}, shutting down...", LogLevel.Fatal);
                Environment.Exit(1);
            }

            // Set up the VFS
            _resources.Initialize();

#if RELEASE
            _resources.MountContentDirectory(@"./Resources/");
#else
            // Load from the resources dir in the repo root instead.
            // It's a debug build so this is fine.
            _resources.MountContentDirectory(@"../../Resources/");
            _resources.MountContentDirectory(@"Resources/Assemblies", "Assemblies/");
#endif

            //mount the engine content pack
            // _resources.MountContentPack(@"EngineContentPack.zip");

            //mount the default game ContentPack defined in config
            // _resources.MountDefaultContentPack();

            //identical code in game controller for client
            if (!AssemblyLoader.TryLoadAssembly<GameShared>(_resources, $"Content.Shared"))
                if (!AssemblyLoader.TryLoadAssembly<GameShared>(_resources, $"Sandbox.Shared"))
                    Logger.Warning($"[ENG] Could not load any Shared DLL.");

            if (!AssemblyLoader.TryLoadAssembly<GameServer>(_resources, $"Content.Server"))
                if (!AssemblyLoader.TryLoadAssembly<GameServer>(_resources, $"Sandbox.Server"))
                    Logger.Warning($"[ENG] Could not load any Server DLL.");

            // HAS to happen after content gets loaded.
            // Else the content types won't be included.
            // TODO: solve this properly.
            _serializer.Initialize();

            // Initialize Tier 2 services
            IoCManager.Resolve<IGameStateManager>().Initialize();
            IoCManager.Resolve<IEntityManager>().Initialize();
            IoCManager.Resolve<IChatManager>().Initialize();
            IoCManager.Resolve<IPlayerManager>().Initialize(MaxPlayers);
            IoCManager.Resolve<IMapManager>().Initialize();
            IoCManager.Resolve<IPlacementManager>().Initialize();

            // Call Init in game assemblies.
            AssemblyLoader.BroadcastRunLevel(AssemblyLoader.RunLevel.Init);

            IoCManager.Resolve<ITileDefinitionManager>().Initialize();

            // because of 'reasons' this has to be called after the last assembly is loaded
            // otherwise the prototypes will be cleared
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            prototypeManager.LoadDirectory(@"Prototypes");
            prototypeManager.Resync();

            var clientConsole = IoCManager.Resolve<IClientConsoleHost>();
            clientConsole.Initialize();
            var consoleManager = IoCManager.Resolve<IConsoleManager>();
            consoleManager.Initialize();

            OnRunLevelChanged(ServerRunLevel.PreGame);

            return false;
        }

        private TimeSpan _lastTick;
        private TimeSpan _lastKeepUpAnnounce;

        /// <inheritdoc />
        public void MainLoop()
        {
            _mainLoop = new GameLoop(_time);
            _mainLoop.SleepMode = SleepMode.Delay;

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
            Console.Title = string.Format("FPS: {0:N2} SD: {1:N2}ms | Net: ({2}) | Memory: {3:N0} KiB",
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

            cfgMgr.RegisterCVar("net.tickrate", 66, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);

            cfgMgr.RegisterCVar("game.hostname", "MyServer", CVar.ARCHIVE);
            cfgMgr.RegisterCVar("game.mapname", "SavedEntities.xml", CVar.ARCHIVE);
            cfgMgr.RegisterCVar("game.maxplayers", 32, CVar.ARCHIVE);
            cfgMgr.RegisterCVar("game.type", GameType.Game);
            cfgMgr.RegisterCVar("game.welcomemsg", "Welcome to the server!", CVar.ARCHIVE);

            _time.TickRate = _config.GetCVar<int>("net.tickrate");

            Logger.Info($"[SRV] Name: {ServerName}");
            Logger.Info($"[SRV] TickRate: {_time.TickRate}({_time.TickPeriod.TotalMilliseconds:0.00}ms)");
            Logger.Info($"[SRV] Map: {MapName}");
            Logger.Info($"[SRV] Max players: {MaxPlayers}");
            Logger.Info($"[SRV] Welcome message: {Motd}");
        }

        /// <summary>
        ///     Switches the run level of the BaseServer to the desired value.
        /// </summary>
        private void OnRunLevelChanged(ServerRunLevel level)
        {
            if (level == _runLevel)
                return;

            Logger.Debug($"[ENG] Runlevel changed to: {level}");
            var args = new RunLevelChangedEventArgs(_runLevel, level);
            _runLevel = level;
            RunLevelChanged?.Invoke(this, args);

            // positive edge triggers
            switch (level)
            {
                case ServerRunLevel.PreGame:
                    _entities.Startup();
                    break;
            }
        }

        // called right before main loop returns, do all saving/cleanup in here
        private void Cleanup()
        {
            // shut down networking, kicking all players.
            _network.Shutdown("Server Shutdown");

            // shutdown entities
            _entities.Shutdown();

            //TODO: This should prob shutdown all managers in a loop.

            // remove all maps
            if(_runLevel == ServerRunLevel.Game)
            {
                var mapMgr = IoCManager.Resolve<IMapManager>();

                // TODO: Unregister all maps.
                mapMgr.DeleteMap(new MapId(1));
            }
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

            IoCManager.Resolve<IServerNetManager>().ProcessPackets();

            AssemblyLoader.BroadcastUpdate(AssemblyLoader.UpdateLevel.PreEngine, frameTime);

            timerManager.UpdateTimers(frameTime);
            if (_runLevel >= ServerRunLevel.PreGame)
            {
                _components.Update(frameTime);
                _entities.Update(frameTime);
            }
            AssemblyLoader.BroadcastUpdate(AssemblyLoader.UpdateLevel.PostEngine, frameTime);

            _stateManager.SendGameStateUpdate();
        }
    }

    /// <summary>
    ///     Enumeration of the run levels of the BaseServer.
    /// </summary>
    public enum ServerRunLevel
    {
        Error = 0,
        Init,
        PreGame,
        Game,
        PostGame,
        MapChange,
    }

    /// <summary>
    ///     Type of game currently running.
    /// </summary>
    public enum GameType
    {
        MapEditor = 0,
        Game,
    }

    /// <summary>
    ///     Event arguments for when the RunLevel has changed in the BaseServer.
    /// </summary>
    public class RunLevelChangedEventArgs : EventArgs
    {
        /// <summary>
        ///     RunLevel that the BaseServer switched from.
        /// </summary>
        public ServerRunLevel OldLevel { get; }

        /// <summary>
        ///     RunLevel that the BaseServers switched to.
        /// </summary>
        public ServerRunLevel NewLevel { get; }

        /// <summary>
        ///     Constructs a new instance of the class.
        /// </summary>
        public RunLevelChangedEventArgs(ServerRunLevel oldLevel, ServerRunLevel newLevel)
        {
            OldLevel = oldLevel;
            NewLevel = newLevel;
        }
    }
}
