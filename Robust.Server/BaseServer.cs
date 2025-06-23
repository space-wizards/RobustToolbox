using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using Prometheus;
using Robust.Server.Console;
using Robust.Server.DataMetrics;
using Robust.Server.GameObjects;
using Robust.Server.GameStates;
using Robust.Server.Log;
using Robust.Server.Placement;
using Robust.Server.Player;
using Robust.Server.Scripting;
using Robust.Server.ServerHub;
using Robust.Server.ServerStatus;
using Robust.Server.Upload;
using Robust.Server.Utility;
using Robust.Server.ViewVariables;
using Robust.Shared;
using Robust.Shared.Asynchronous;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Enums;
using Robust.Shared.Exceptions;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Profiling;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;
using Robust.Shared.Replays;
using Robust.Shared.Toolshed;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Threading;
using Robust.Shared.Timing;
using Robust.Shared.Upload;
using Robust.Shared.Utility;
using Serilog.Debugging;
using Serilog.Sinks.Loki;

namespace Robust.Server
{
    /// <summary>
    /// The master class that runs the rest of the engine.
    /// </summary>
    internal sealed class BaseServer : IBaseServerInternal, IPostInjectInit
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

        [Dependency] private readonly INetConfigurationManagerInternal _config = default!;
        [Dependency] private readonly IServerEntityManager _entityManager = default!;
        [Dependency] private readonly ILogManager _log = default!;
        [Dependency] private readonly IRobustSerializer _serializer = default!;
        [Dependency] private readonly IGameTiming _time = default!;
        [Dependency] private readonly IResourceManagerInternal _resources = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly ITimerManager _timerManager = default!;
        [Dependency] private readonly IServerGameStateManager _stateManager = default!;
        [Dependency] private readonly IServerNetManager _network = default!;
        [Dependency] private readonly ISystemConsoleManager _systemConsole = default!;
        [Dependency] private readonly ITaskManager _taskManager = default!;
        [Dependency] private readonly IRuntimeLog _runtimeLog = default!;
        [Dependency] private readonly IModLoaderInternal _modLoader = default!;
        [Dependency] private readonly IWatchdogApiInternal _watchdogApi = default!;
        [Dependency] private readonly HubManager _hubManager = default!;
        [Dependency] private readonly IScriptHost _scriptHost = default!;
        [Dependency] private readonly IMetricsManagerInternal _metricsManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IRobustMappedStringSerializer _stringSerializer = default!;
        [Dependency] private readonly ILocalizationManagerInternal _loc = default!;
        [Dependency] private readonly IServerConsoleHost _consoleHost = default!;
        [Dependency] private readonly IParallelManagerInternal _parallelMgr = default!;
        [Dependency] private readonly ProfManager _prof = default!;
        [Dependency] private readonly IPrototypeManagerInternal _prototype = default!;
        [Dependency] private readonly IPlacementManager _placement = default!;
        [Dependency] private readonly IServerViewVariablesInternal _viewVariables = default!;
        [Dependency] private readonly ISerializationManager _serialization = default!;
        [Dependency] private readonly IStatusHost _statusHost = default!;
        [Dependency] private readonly IComponentFactory _componentFactory = default!;
        [Dependency] private readonly IReplayRecordingManagerInternal _replay = default!;
        [Dependency] private readonly IGamePrototypeLoadManager _protoLoadMan = default!;
        [Dependency] private readonly UploadedContentManager _uploadedContMan = default!;
        [Dependency] private readonly NetworkResourceManager _netResMan = default!;
        [Dependency] private readonly IReflectionManager _refMan = default!;

        private readonly Stopwatch _uptimeStopwatch = new();

        private CommandLineArgs? _commandLineArgs;
        private Func<ILogHandler>? _logHandlerFactory;
        private ILogHandler? _logHandler;
        private IGameLoop _mainLoop = default!;
        private ISawmill _logger = default!;
        private bool _autoPause;

        private string? _shutdownReason;

        private readonly ManualResetEventSlim _shutdownEvent = new(false);

        public ServerOptions Options { get; private set; } = new();

        /// <inheritdoc />
        public int MaxPlayers => _config.GetEffectiveMaxConnections();

        /// <inheritdoc />
        public string ServerName => _config.GetCVar(CVars.GameHostName);

        public bool ContentStart { get; set; }

        /// <inheritdoc />
        public void Restart()
        {
            // FIXME: This explodes very violently.
            Cleanup();
            Start(Options, _logHandlerFactory);
        }

        /// <inheritdoc />
        public void Shutdown(string? reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                _logger.Info("Shutting down...");
            else
                _logger.Info($"{reason}, shutting down...");

            _shutdownReason = reason ?? "Shutting down";

            if (_mainLoop != null) _mainLoop.Running = false;
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
        public bool Start(ServerOptions options, Func<ILogHandler>? logHandlerFactory = null)
        {
            Options = options;
            _config.Initialize(true);

            if (Options.LoadConfigAndUserData)
            {
                string? path = _commandLineArgs?.ConfigFile;

                // Sets up the configMgr
                // If a config file path was passed, use it literally.
                // This ensures it's working-directory relative
                // (for people passing config file through the terminal or something).
                // Otherwise use the one next to the executable.
                if (string.IsNullOrEmpty(path))
                {
                    path = PathHelpers.ExecutableRelativeFile("server_config.toml");
                }

                if (File.Exists(path))
                {
                    _config.LoadFromFile(path);
                }
                else
                {
                    _config.SetSaveFile(path);
                }
            }

            _config.LoadCVarsFromAssembly(typeof(BaseServer).Assembly); // Robust.Server
            _config.LoadCVarsFromAssembly(typeof(IConfigurationManager).Assembly); // Robust.Shared

            CVarDefaultOverrides.OverrideServer(_config);

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
                    fullPath = PathHelpers.ExecutableRelativeFile(fullPath);
                }

                logHandler = new FileLogHandler(fullPath);
            }

            _log.RootSawmill.Level = _config.GetCVar(CVars.LogLevel);

            if (logEnabled && logHandler != null)
            {
                _logHandler = logHandler;
                _log.RootSawmill.AddHandler(_logHandler!);
            }

            if (_commandLineArgs != null)
            {
                foreach (var (sawmill, level) in _commandLineArgs.LogLevels)
                {
                    LogLevel? logLevel;
                    if (level == "null")
                        logLevel = null;
                    else
                    {
                        if (!Enum.TryParse<LogLevel>(level, out var result))
                        {
                            System.Console.WriteLine($"LogLevel {level} does not exist!");
                            continue;
                        }
                        logLevel = result;
                    }
                    _log.GetSawmill(sawmill).Level = logLevel;
                }
            }

            ProgramShared.PrintRuntimeInfo(_log.RootSawmill);

            SelfLog.Enable(s => { System.Console.WriteLine("SERILOG ERROR: {0}", s); });

            if (!SetupLoki())
            {
                return true;
            }

            // Has to be done early because this guy's in charge of the main thread Synchronization Context.
            _taskManager.Initialize();
            _parallelMgr.Initialize();

            LoadSettings();

            // Load metrics really early so that we can profile startup times in the future maybe.
            _metricsManager.Initialize();

            try
            {
                _network.Initialize(true);
                _network.StartServer();
            }
            catch (SocketException e) when (e.SocketErrorCode == SocketError.AddressAlreadyInUse)
            {
                var port = _network.Port;
                _logger.Fatal("Unable to setup networking manager. Make sure that you aren't running the server twice and that port {0} is not in use by another application.\n{1}", port, e);
                return true;
            }
            catch (Exception e)
            {
                _logger.Fatal("Unable to setup networking manager!\n{0}", e);
                return true;
            }

            var dataDir = Options.LoadConfigAndUserData
                ? _commandLineArgs?.DataDir ?? PathHelpers.ExecutableRelativeFile("data")
                : null;

            // Set up the VFS
            _resources.Initialize(dataDir);

            var mountOptions = _commandLineArgs != null
                ? MountOptions.Merge(_commandLineArgs.MountOptions, Options.MountOptions) : Options.MountOptions;

            ProgramShared.DoMounts(_resources, mountOptions, Options.ContentBuildDirectory, Options.AssemblyDirectory,
                Options.LoadContentResources, Options.ResourceMountDisabled, ContentStart);

            // When the game is ran with the startup executable being content,
            // we have to disable the separate load context.
            // Otherwise the content assemblies will be loaded twice which causes *many* fun bugs.
            _modLoader.SetUseLoadContext(!ContentStart);
            _modLoader.SetEnableSandboxing(Options.Sandboxing);

            var resourceManifest = ResourceManifestData.LoadResourceManifest(_resources);

            if (!_modLoader.TryLoadModulesFrom(Options.AssemblyDirectory, resourceManifest.AssemblyPrefix ?? Options.ContentModulePrefix))
            {
                _logger.Fatal("Errors while loading content assemblies.");
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

            _loc.Initialize();
            _loc.AddLoadedToStringSerializer(_stringSerializer);

            //IoCManager.Resolve<IMapLoader>().LoadedMapData +=
            //    IoCManager.Resolve<IRobustMappedStringSerializer>().AddStrings;
            _prototype.LoadedData += data =>
            {
                if (!_stringSerializer.Locked)
                {
                    _stringSerializer.AddStrings(data.Root);
                }
            };

            // Initialize Tier 2 services
            _time.InSimulation = true;

            _config.SetupNetworking();
            _playerManager.Initialize(MaxPlayers);
            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
            _placement.Initialize();
            _viewVariables.Initialize();

            // Call Init in game assemblies.
            _modLoader.BroadcastRunLevel(ModRunLevel.Init);

            // Start bad file extensions check after content init,
            // in case content screws with the VFS.
            var checkBadExtensions = ProgramShared.CheckBadFileExtensions(
                _resources,
                _config,
                _log.GetSawmill("res"));

            _entityManager.Initialize();
            _mapManager.Initialize();

            _serialization.Initialize();

            // Make sure this is done before we try to load prototypes,
            // avoid any possibility of race conditions causing the check to not finish
            // before prototype load and maybe break something.
            ProgramShared.FinishCheckBadFileExtensions(checkBadExtensions);

            // because of 'reasons' this has to be called after the last assembly is loaded
            // otherwise the prototypes will be cleared
            _prototype.Initialize();
            _prototype.LoadDefaultPrototypes();
            _refMan.Initialize();

            IoCManager.Resolve<ToolshedManager>().Initialize();
            _consoleHost.Initialize();
            _entityManager.Startup();
            _mapManager.Startup();
            _stateManager.Initialize();
            _replay.Initialize();

            var reg = _entityManager.ComponentFactory.GetRegistration<TransformComponent>();
            if (!reg.NetID.HasValue)
                throw new InvalidOperationException("TransformComponent does not have a NetId.");

            _stateManager.TransformNetId = reg.NetID.Value;

            _scriptHost.Initialize();
            _protoLoadMan.Initialize();
            _netResMan.Initialize();
            _uploadedContMan.Initialize();

            // String serializer has to be locked before PostInit as content can depend on it (e.g., replays that start
            // automatically recording on startup).
            AddFinalStringsToSerializer();
            _stringSerializer.LockStrings();

            _modLoader.BroadcastRunLevel(ModRunLevel.PostInit);

            _statusHost.Start();
            _hubManager.Start();

            AppDomain.CurrentDomain.ProcessExit += ProcessExiting;

            _watchdogApi.Initialize();

            if (OperatingSystem.IsWindows() && _config.GetCVar(CVars.SysWinTickPeriod) >= 0)
            {
                WindowsTickPeriod.TimeBeginPeriod((uint) _config.GetCVar(CVars.SysWinTickPeriod));
            }

            _config.CheckUnusedCVars();

            if (_config.GetCVar(CVars.SysGCCollectStart))
            {
                GC.Collect();
            }

            ProgramShared.RunExecCommands(_consoleHost, _commandLineArgs?.ExecCommands);

            return false;
        }

        private void AddFinalStringsToSerializer()
        {
            foreach (var regType in _componentFactory.AllRegisteredTypes)
            {
                var reg = _componentFactory.GetRegistration(regType);
                _stringSerializer.AddString(reg.Name);
            }

            using var extraMappedStrings = typeof(BaseServer).Assembly
                .GetManifestResourceStream("Robust.Server.ExtraMappedSerializerStrings.txt");

            if (extraMappedStrings != null)
            {
                using var sr = new StreamReader(extraMappedStrings);
                string? line;
                while ((line = sr.ReadLine()) != null)
                {
                    _stringSerializer.AddString(line);
                }
            }
        }

        private bool SetupLoki()
        {
            var enabled = _config.GetCVar(CVars.LokiEnabled);
            if (!enabled)
            {
                return true;
            }

            var logger = _log.GetSawmill("loki");
            var serverName = _config.GetCVar(CVars.LokiName);
            var address = _config.GetCVar(CVars.LokiAddress);
            var username = _config.GetCVar(CVars.LokiUsername);
            var password = _config.GetCVar(CVars.LokiPassword);

            if (string.IsNullOrWhiteSpace(serverName))
            {
                logger.Fatal("Misconfiguration: Server name is not specified/empty.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(address))
            {
                logger.Fatal("Misconfiguration: Loki address is not specified/empty.");
                return false;
            }

            LokiSinkConfiguration cfg = new()
            {
                LokiUrl = address
            };

            if (!string.IsNullOrWhiteSpace(username))
            {
                if (string.IsNullOrWhiteSpace(password))
                {
                    logger.Fatal("Misconfiguration: Loki password is not specified/empty but username is.");
                    return false;
                }

                cfg.LokiUsername = username;
                cfg.LokiPassword = password;
            }

            cfg.LogLabelProvider = new LogLabelProvider(serverName);

            logger.Debug("Loki enabled for server {ServerName} loki address {LokiAddress}.", serverName,
                address);

            var handler = new LokiLogHandler(cfg);
            _log.RootSawmill.AddHandler(handler);
            return true;
        }

        private void ProcessExiting(object? sender, EventArgs e)
        {
            // If the main loop is not running the task will never get processed on the main thread
            if (!_mainLoop.Running)
            {
                return;
            }

            _taskManager.RunOnMainThread(() => Shutdown("ProcessExited"));
            // Give the server 10 seconds to shut down.
            // If it still hasn't managed to assume it's stuck or something.
            if (!_shutdownEvent.Wait(10_000))
            {
                System.Console.WriteLine("ProcessExited timeout (10s) has been passed; killing server.");
                // This kills the server right? Returning?
            }
        }

        internal void SetupMainLoop()
        {
            if (_mainLoop == null)
            {
                _mainLoop = new GameLoop(
                    _time,
                    _runtimeLog,
                    _prof,
                    _log.GetSawmill("eng"),
                    GameLoopOptions.FromCVars(_config))
                {
                    SleepMode = SleepMode.Delay,
                    DetectSoftLock = true,
                    EnableMetrics = true
                };
            }

            _uptimeStopwatch.Start();

            _mainLoop.Input += (sender, args) => Input(args);

            _mainLoop.Tick += (sender, args) => Update(args);

            _mainLoop.Update += (sender, args) => FrameUpdate(args);

            // set GameLoop.Running to false to return from this function.
            _time.Paused = _autoPause;
        }

        internal void FinishMainLoop()
        {
            _time.InSimulation = true;
            Cleanup();

            _shutdownEvent.Set();
        }

        /// <inheritdoc />
        public void MainLoop()
        {
            SetupMainLoop();

            // If the server has been given a reason to shut down before the main loop has started,
            // Don't start the main loop. This only works if a reason is passed to Shutdown(...)
            if (_shutdownReason != null)
            {
                _logger.Fatal("Shutdown has been requested before the main loop has been started, complying. Reason: {0}", _shutdownReason);
            }
            else _mainLoop.Run();

            FinishMainLoop();
        }

        public void OverrideMainLoop(IGameLoop gameLoop)
        {
            _mainLoop = gameLoop;
        }


        /// <summary>
        ///     Loads the server settings from the ConfigurationManager.
        /// </summary>
        private void LoadSettings()
        {
            _config.OnValueChanged(CVars.NetTickrate, i =>
            {
                var b = (ushort) i;
                _time.TickRate = b;

                _logger.Info($"Tickrate changed to: {b} on tick {_time.CurTick}");
            });

            var startOffset = TimeSpan.FromSeconds(_config.GetCVar(CVars.NetTimeStartOffset));
            _time.TimeBase = (startOffset, GameTick.First);
            _time.TickRate = (ushort) _config.GetCVar(CVars.NetTickrate);

            _logger.Info($"Name: {ServerName}");
            _logger.Info($"TickRate: {_time.TickRate}({_time.TickPeriod.TotalMilliseconds:0.00}ms)");
            _logger.Info($"Max players: {MaxPlayers}");

            _config.OnValueChanged(CVars.GameAutoPauseEmpty, UpdateAutoPause, true);
        }

        private void UpdateAutoPause(bool doAutoPause)
        {
            _autoPause = doAutoPause;
            if (doAutoPause)
            {
                if (!_time.Paused && CheckIfShouldAutoPause())
                {
                    _logger.Debug("game.auto_pause_empty changed, pausing");
                    _time.Paused = true;
                }
            }
            else if (_time.Paused)
            {
                _logger.Debug("game.auto_pause_empty changed, unpausing");
                _time.Paused = false;
            }
        }

        private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
        {
            if (!_autoPause)
                return;

            if (e.NewStatus == SessionStatus.Connected && _time.Paused)
            {
                _logger.Debug("Client connecting, unpausing automatically.");
                _time.Paused = false;
            }

            if (e.NewStatus == SessionStatus.Disconnected && CheckIfShouldAutoPause())
            {
                _logger.Debug("Last client disconnected, pausing automatically.");
                _time.Paused = true;
            }
        }

        private bool CheckIfShouldAutoPause()
        {
            return _playerManager.Sessions.All(s => s.Status == SessionStatus.Disconnected);
        }

        // called right before main loop returns, do all saving/cleanup in here
        public void Cleanup()
        {
            _replay.Shutdown();

            _modLoader.Shutdown();

            _playerManager.Shutdown();
            _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;

            // shut down networking, kicking all players.
            var shutdownReasonWithRedial = new NetDisconnectMessage($"Server shutting down: {_shutdownReason}", true);
            _network.Shutdown(shutdownReasonWithRedial.Encode());

            // shutdown entities
            _entityManager.Cleanup();

            if (_config.GetCVar(CVars.LogRuntimeLog))
            {
                // Write down exception log
                var logPath = _config.GetCVar(CVars.LogPath);
                if (!Path.IsPathRooted(logPath))
                {
                    logPath = PathHelpers.ExecutableRelativeFile(logPath);
                }

                var pathToWrite = Path.Combine(logPath,
                    "Runtime-" + DateTime.Now.ToString("yyyy-MM-dd-THH-mm-ss") + ".txt");
                Directory.CreateDirectory(logPath);
                File.WriteAllText(pathToWrite, _runtimeLog.Display(), EncodingHelpers.UTF8);
            }

            AppDomain.CurrentDomain.ProcessExit -= ProcessExiting;

            //TODO: This should prob shutdown all managers in a loop.

            if (OperatingSystem.IsWindows() && _config.GetCVar(CVars.SysWinTickPeriod) >= 0)
            {
                WindowsTickPeriod.TimeEndPeriod((uint) _config.GetCVar(CVars.SysWinTickPeriod));
            }

            _config.Shutdown();
        }

        private void Input(FrameEventArgs args)
        {
            using var _ = TickUsage.WithLabels("Inputs").NewTimer();
            _systemConsole.UpdateInput();

            _network.ProcessPackets();
            _taskManager.ProcessPendingTasks();

            _modLoader.BroadcastUpdate(ModUpdateLevel.InputPostEngine, args);
        }

        private void Update(FrameEventArgs frameEventArgs)
        {
            ServerCurTick.Set(_time.CurTick.Value);
            ServerCurTime.Set(_time.CurTime.TotalSeconds);

            _systemConsole.UpdateTick();

            using (TickUsage.WithLabels("PreEngine").NewTimer())
            {
                _modLoader.BroadcastUpdate(ModUpdateLevel.PreEngine, frameEventArgs);
            }

            using (TickUsage.WithLabels("NetworkedCVar").NewTimer())
            {
                _config.TickProcessMessages();
            }

            using (TickUsage.WithLabels("Timers").NewTimer())
            {
                _consoleHost.CommandBufferExecute();
                _timerManager.UpdateTimers(frameEventArgs);
            }

            using (TickUsage.WithLabels("AsyncTasks").NewTimer())
            {
                _taskManager.ProcessPendingTasks();
            }

            // Pass Histogram into the IEntityManager.Update so it can do more granular measuring.
            _entityManager.TickUpdate(frameEventArgs.DeltaSeconds, noPredictions: false, TickUsage);

            using (TickUsage.WithLabels("PostEngine").NewTimer())
            {
                _modLoader.BroadcastUpdate(ModUpdateLevel.PostEngine, frameEventArgs);
            }

            using (TickUsage.WithLabels("GameState").NewTimer())
            {
                _stateManager.SendGameStateUpdate();
            }
        }

        private void FrameUpdate(FrameEventArgs frameEventArgs)
        {
            ServerUpTime.Set(_uptimeStopwatch.Elapsed.TotalSeconds);

            _modLoader.BroadcastUpdate(ModUpdateLevel.FramePreEngine, frameEventArgs);

            _watchdogApi.Heartbeat();
            _hubManager.Heartbeat();

            _modLoader.BroadcastUpdate(ModUpdateLevel.FramePostEngine, frameEventArgs);

            _metricsManager.FrameUpdate();
        }

        void IPostInjectInit.PostInject()
        {
            _logger = _log.GetSawmill("srv");
        }
    }
}
