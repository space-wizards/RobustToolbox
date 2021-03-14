using System;
using System.Diagnostics;
using System.IO;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Threading;
using Prometheus;
using Robust.Server.Console;
using Robust.Server.DataMetrics;
using Robust.Server.Debugging;
using Robust.Server.GameObjects;
using Robust.Server.GameStates;
using Robust.Server.Log;
using Robust.Server.Placement;
using Robust.Server.Player;
using Robust.Server.Scripting;
using Robust.Server.ServerStatus;
using Robust.Server.Utility;
using Robust.Server.ViewVariables;
using Robust.Shared;
using Robust.Shared.Asynchronous;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Exceptions;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Serilog.Debugging;
using Serilog.Sinks.Loki;
using Stopwatch = Robust.Shared.Timing.Stopwatch;

namespace Robust.Server
{
    /// <summary>
    /// The master class that runs the rest of the engine.
    /// </summary>
    internal sealed class BaseServer : IBaseServerInternal
    {
        private static readonly Gauge ServerUpTime = Metrics.CreateGauge(
            "robust_server_uptime",
            "The real time the server main loop has been running.");

        private static readonly Gauge ServerCurTime = Metrics.CreateGauge(
            "robust_server_curtime",
            "The IGameTiming.CurTime of the server.");

        private static readonly Gauge ServerCurTick = Metrics.CreateGauge(
            "robust_server_curtick",
            "The IGameTiming.CurTick of the server.");

        private static readonly Histogram TickUsage = Metrics.CreateHistogram(
            "robust_server_update_usage",
            "Time usage of the main loop Update()s",
            new HistogramConfiguration
            {
                LabelNames = new[] {"area"},
                Buckets = Histogram.ExponentialBuckets(0.000_01, 2, 13)
            });

        [Dependency] private readonly IConfigurationManagerInternal _config = default!;
        [Dependency] private readonly IComponentManager _components = default!;
        [Dependency] private readonly IServerEntityManager _entities = default!;
        [Dependency] private readonly ILogManager _log = default!;
        [Dependency] private readonly IRobustSerializer _serializer = default!;
        [Dependency] private readonly IGameTiming _time = default!;
        [Dependency] private readonly IResourceManagerInternal _resources = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly ITimerManager timerManager = default!;
        [Dependency] private readonly IServerGameStateManager _stateManager = default!;
        [Dependency] private readonly IServerNetManager _network = default!;
        [Dependency] private readonly ISystemConsoleManager _systemConsole = default!;
        [Dependency] private readonly ITaskManager _taskManager = default!;
        [Dependency] private readonly IRuntimeLog runtimeLog = default!;
        [Dependency] private readonly IModLoaderInternal _modLoader = default!;
        [Dependency] private readonly IWatchdogApi _watchdogApi = default!;
        [Dependency] private readonly IScriptHost _scriptHost = default!;
        [Dependency] private readonly IMetricsManager _metricsManager = default!;
        [Dependency] private readonly IRobustMappedStringSerializer _stringSerializer = default!;
        [Dependency] private readonly ILocalizationManagerInternal _loc = default!;

        private readonly Stopwatch _uptimeStopwatch = new();

        private CommandLineArgs? _commandLineArgs;
        private Func<ILogHandler>? _logHandlerFactory;
        private ILogHandler? _logHandler;
        private IGameLoop _mainLoop = default!;

        private TimeSpan _lastTitleUpdate;
        private int _lastReceivedBytes;
        private int _lastSentBytes;

        private string? _shutdownReason;

        private readonly ManualResetEventSlim _shutdownEvent = new(false);

        /// <inheritdoc />
        public int MaxPlayers => _config.GetCVar(CVars.GameMaxPlayers);

        /// <inheritdoc />
        public string ServerName => _config.GetCVar(CVars.GameHostName);

        /// <inheritdoc />
        public void Restart()
        {
            Logger.InfoS("srv", "Restarting Server...");

            Cleanup();
            Start(_logHandlerFactory);
        }

        /// <inheritdoc />
        public void Shutdown(string? reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                Logger.InfoS("srv", "Shutting down...");
            else
                Logger.InfoS("srv", $"{reason}, shutting down...");

            _shutdownReason = reason;

            _mainLoop.Running = false;
            if (_logHandler != null)
            {
                _log.RootSawmill.RemoveHandler(_logHandler);
                (_logHandler as IDisposable)?.Dispose();
            }
        }

        public void SetCommandLineArgs(CommandLineArgs args)
        {
            _commandLineArgs = args;
        }

        /// <inheritdoc />
        public bool Start(Func<ILogHandler>? logHandlerFactory = null)
        {
            var profilePath = Path.Join(Environment.CurrentDirectory, "AAAAAAAA");
            ProfileOptimization.SetProfileRoot(profilePath);
            ProfileOptimization.StartProfile("AAAAAAAAAA");

            _config.Initialize(true);

            if (LoadConfigAndUserData)
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
            }

            _config.LoadCVarsFromAssembly(typeof(BaseServer).Assembly); // Robust.Server
            _config.LoadCVarsFromAssembly(typeof(IConfigurationManager).Assembly); // Robust.Shared

            _config.OverrideConVars(EnvironmentVariables.GetEnvironmentCVars());

            if (_commandLineArgs != null)
            {
                _config.OverrideConVars(_commandLineArgs.CVars);
            }

            ProfileOptSetup.Setup(_config);

            //Sets up Logging
            _logHandlerFactory = logHandlerFactory;

            var logHandler = logHandlerFactory?.Invoke() ?? null;

            var logEnabled = _config.GetCVar(CVars.LogEnabled);

            if (logEnabled && logHandler == null)
            {
                var logPath = _config.GetCVar(CVars.LogPath);
                var logFormat = _config.GetCVar(CVars.LogFormat);
                var logFilename = logFormat.Replace("%(date)s", DateTime.Now.ToString("yyyy-MM-dd"))
                    .Replace("%(time)s", DateTime.Now.ToString("hh-mm-ss"));
                var fullPath = Path.Combine(logPath, logFilename);

                if (!Path.IsPathRooted(fullPath))
                {
                    logPath = PathHelpers.ExecutableRelativeFile(fullPath);
                }

                logHandler = new FileLogHandler(logPath);
            }

            _log.RootSawmill.Level = _config.GetCVar(CVars.LogLevel);

            if (logEnabled && logHandler != null)
            {
                _logHandler = logHandler;
                _log.RootSawmill.AddHandler(_logHandler!);
            }

            SelfLog.Enable(s => { System.Console.WriteLine("SERILOG ERROR: {0}", s); });

            if (!SetupLoki())
            {
                return true;
            }

            // Has to be done early because this guy's in charge of the main thread Synchronization Context.
            _taskManager.Initialize();

            LoadSettings();

            // Load metrics really early so that we can profile startup times in the future maybe.
            _metricsManager.Initialize();

            var netMan = IoCManager.Resolve<IServerNetManager>();
            try
            {
                netMan.Initialize(true);
                netMan.StartServer();
            }
            catch (Exception e)
            {
                var port = netMan.Port;
                Logger.Fatal(
                    "Unable to setup networking manager. Check port {0} is not already in use and that all binding addresses are correct!\n{1}",
                    port, e);
                return true;
            }

            var dataDir = LoadConfigAndUserData
                ? _commandLineArgs?.DataDir ?? PathHelpers.ExecutableRelativeFile("data")
                : null;

            // Set up the VFS
            _resources.Initialize(dataDir);

            ProgramShared.DoMounts(_resources, _commandLineArgs?.MountOptions, "Content.Server");

            _modLoader.SetUseLoadContext(!DisableLoadContext);
            _modLoader.SetEnableSandboxing(false);

            if (!_modLoader.TryLoadModulesFrom(new ResourcePath("/Assemblies/"), "Content."))
            {
                Logger.Fatal("Errors while loading content assemblies.");
                return true;
            }

            foreach (var loadedModule in _modLoader.LoadedModules)
            {
                _config.LoadCVarsFromAssembly(loadedModule);
            }

            _modLoader.BroadcastRunLevel(ModRunLevel.PreInit);

            // HAS to happen after content gets loaded.
            // Else the content types won't be included.
            // TODO: solve this properly.
            _serializer.Initialize();

            _loc.AddLoadedToStringSerializer(_stringSerializer);

            //IoCManager.Resolve<IMapLoader>().LoadedMapData +=
            //    IoCManager.Resolve<IRobustMappedStringSerializer>().AddStrings;
            IoCManager.Resolve<IPrototypeManager>().LoadedData += (yaml, name) =>
            {
                if (!_stringSerializer.Locked)
                {
                    _stringSerializer.AddStrings(yaml);
                }
            };

            // Initialize Tier 2 services
            IoCManager.Resolve<IGameTiming>().InSimulation = true;

            IoCManager.Resolve<INetConfigurationManager>().SetupNetworking();

            _stateManager.Initialize();
            IoCManager.Resolve<IPlayerManager>().Initialize(MaxPlayers);
            _mapManager.Initialize();
            _mapManager.Startup();
            IoCManager.Resolve<IPlacementManager>().Initialize();
            IoCManager.Resolve<IViewVariablesHost>().Initialize();
            IoCManager.Resolve<IDebugDrawingManager>().Initialize();

            // Call Init in game assemblies.
            _modLoader.BroadcastRunLevel(ModRunLevel.Init);

            _entities.Initialize();

            IoCManager.Resolve<ISerializationManager>().Initialize();

            // because of 'reasons' this has to be called after the last assembly is loaded
            // otherwise the prototypes will be cleared
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            prototypeManager.Initialize();
            prototypeManager.LoadDirectory(new ResourcePath(@"/Prototypes"));
            prototypeManager.Resync();

            IoCManager.Resolve<IServerConsoleHost>().Initialize();
            _entities.Startup();
            _scriptHost.Initialize();

            _modLoader.BroadcastRunLevel(ModRunLevel.PostInit);

            IoCManager.Resolve<IStatusHost>().Start();

            AppDomain.CurrentDomain.ProcessExit += ProcessExiting;

            _watchdogApi.Initialize();

            _stringSerializer.LockStrings();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && _config.GetCVar(CVars.SysWinTickPeriod) >= 0)
            {
                WindowsTickPeriod.TimeBeginPeriod((uint) _config.GetCVar(CVars.SysWinTickPeriod));
            }

            GC.Collect();

            return false;
        }

        private bool SetupLoki()
        {
            var enabled = _config.GetCVar(CVars.LokiEnabled);
            if (!enabled)
            {
                return true;
            }

            var serverName = _config.GetCVar(CVars.LokiName);
            var address = _config.GetCVar(CVars.LokiAddress);
            var username = _config.GetCVar(CVars.LokiUsername);
            var password = _config.GetCVar(CVars.LokiPassword);

            if (string.IsNullOrWhiteSpace(serverName))
            {
                Logger.FatalS("loki", "Misconfiguration: Server name is not specified/empty.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(address))
            {
                Logger.FatalS("loki", "Misconfiguration: Loki address is not specified/empty.");
                return false;
            }

            LokiCredentials credentials;
            if (string.IsNullOrWhiteSpace(username))
            {
                credentials = new NoAuthCredentials(address);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(password))
                {
                    Logger.FatalS("loki", "Misconfiguration: Loki password is not specified/empty but username is.");
                    return false;
                }

                credentials = new BasicAuthCredentials(address, username, password);
            }

            Logger.DebugS("loki", "Loki enabled for server {ServerName} loki address {LokiAddress}.", serverName,
                address);

            var handler = new LokiLogHandler(serverName, credentials);
            _log.RootSawmill.AddHandler(handler);
            return true;
        }

        private void ProcessExiting(object? sender, EventArgs e)
        {
            _taskManager.RunOnMainThread(() => Shutdown("ProcessExited"));
            // Give the server 10 seconds to shut down.
            // If it still hasn't managed to assume it's stuck or something.
            if (!_shutdownEvent.Wait(10_000))
            {
                System.Console.WriteLine("ProcessExited timeout (10s) has been passed; killing server.");
                // This kills the server right? Returning?
            }
        }

        /// <inheritdoc />
        public async void MainLoop()
        {
            if (_mainLoop == null)
            {
                _mainLoop = new GameLoop(_time)
                {
                    SleepMode = SleepMode.Delay,
                    DetectSoftLock = true,
                    EnableMetrics = true
                };
            }

            _uptimeStopwatch.Start();

            _mainLoop.Input += (sender, args) => Input(args);

            _mainLoop.Tick += (sender, args) => Update(args);

            _mainLoop.Update += (sender, args) => { ServerUpTime.Set(_uptimeStopwatch.Elapsed.TotalSeconds); };

            // set GameLoop.Running to false to return from this function.
            _time.Paused = false;
            await _mainLoop.Run();

            _time.InSimulation = true;
            Cleanup();

            _shutdownEvent.Set();
        }

        public bool DisableLoadContext { private get; set; }
        public bool LoadConfigAndUserData { private get; set; } = true;

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

            cfgMgr.OnValueChanged(CVars.NetTickrate, i =>
            {
                var b = (byte) i;
                _time.TickRate = b;

                Logger.InfoS("game", $"Tickrate changed to: {b} on tick {_time.CurTick}");
            });

            _time.TickRate = (byte) _config.GetCVar(CVars.NetTickrate);

            Logger.InfoS("srv", $"Name: {ServerName}");
            Logger.InfoS("srv", $"TickRate: {_time.TickRate}({_time.TickPeriod.TotalMilliseconds:0.00}ms)");
            Logger.InfoS("srv", $"Max players: {MaxPlayers}");
        }

        // called right before main loop returns, do all saving/cleanup in here
        private void Cleanup()
        {
            IoCManager.Resolve<INetConfigurationManager>().FlushMessages();

            // shut down networking, kicking all players.
            _network.Shutdown($"Server shutting down: {_shutdownReason}");

            // shutdown entities
            _entities.Shutdown();

            if (_config.GetCVar(CVars.LogRuntimeLog))
            {
                // Wrtie down exception log
                var logPath = _config.GetCVar(CVars.LogPath);
                var relPath = PathHelpers.ExecutableRelativeFile(logPath);
                Directory.CreateDirectory(relPath);
                var pathToWrite = Path.Combine(relPath,
                    "Runtime-" + DateTime.Now.ToString("yyyy-MM-dd-THH-mm-ss") + ".txt");
                File.WriteAllText(pathToWrite, runtimeLog.Display(), EncodingHelpers.UTF8);
            }

            AppDomain.CurrentDomain.ProcessExit -= ProcessExiting;

            //TODO: This should prob shutdown all managers in a loop.

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && _config.GetCVar(CVars.SysWinTickPeriod) >= 0)
            {
                WindowsTickPeriod.TimeEndPeriod((uint) _config.GetCVar(CVars.SysWinTickPeriod));
            }
        }

        private string UpdateBps()
        {
            var stats = IoCManager.Resolve<IServerNetManager>().Statistics;

            var bps =
                $"Send: {(stats.SentBytes - _lastSentBytes) >> 10:N0} KiB/s, Recv: {(stats.ReceivedBytes - _lastReceivedBytes) >> 10:N0} KiB/s";

            _lastSentBytes = stats.SentBytes;
            _lastReceivedBytes = stats.ReceivedBytes;

            return bps;
        }

        private void Input(FrameEventArgs args)
        {
            _systemConsole.Update();

            _network.ProcessPackets();
            _taskManager.ProcessPendingTasks();
        }

        private void Update(FrameEventArgs frameEventArgs)
        {
            ServerCurTick.Set(_time.CurTick.Value);
            ServerCurTime.Set(_time.CurTime.TotalSeconds);

            // These are always the same on the server, there is no prediction.
            _time.LastRealTick = _time.CurTick;

            UpdateTitle();

            using (TickUsage.WithLabels("PreEngine").NewTimer())
            {
                _modLoader.BroadcastUpdate(ModUpdateLevel.PreEngine, frameEventArgs);
            }

            using (TickUsage.WithLabels("NetworkedCVar").NewTimer())
            {
                IoCManager.Resolve<INetConfigurationManager>().TickProcessMessages();
            }

            using (TickUsage.WithLabels("Timers").NewTimer())
            {
                timerManager.UpdateTimers(frameEventArgs);
            }

            using (TickUsage.WithLabels("AsyncTasks").NewTimer())
            {
                _taskManager.ProcessPendingTasks();
            }

            using (TickUsage.WithLabels("ComponentCull").NewTimer())
            {
                _components.CullRemovedComponents();
            }

            // Pass Histogram into the IEntityManager.Update so it can do more granular measuring.
            _entities.Update(frameEventArgs.DeltaSeconds, TickUsage);

            using (TickUsage.WithLabels("PostEngine").NewTimer())
            {
                _modLoader.BroadcastUpdate(ModUpdateLevel.PostEngine, frameEventArgs);
            }

            using (TickUsage.WithLabels("GameState").NewTimer())
            {
                _stateManager.SendGameStateUpdate();
            }

            _watchdogApi.Heartbeat();
        }
    }
}
