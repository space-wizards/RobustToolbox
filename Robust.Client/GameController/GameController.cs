using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime;
using System.Threading.Tasks;
using Robust.Client.Audio;
using Robust.Client.Audio.Midi;
using Robust.Client.Console;
using Robust.Client.GameObjects;
using Robust.Client.GameStates;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Placement;
using Robust.Client.Replays.Loading;
using Robust.Client.Replays.Playback;
using Robust.Client.ResourceManagement;
using Robust.Client.State;
using Robust.Client.Upload;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.RichText;
using Robust.Client.UserInterface.XAML.Proxy;
using Robust.Client.Utility;
using Robust.Client.ViewVariables;
using Robust.Client.WebViewHook;
using Robust.LoaderApi;
using Robust.Shared;
using Robust.Shared.Asynchronous;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Exceptions;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Profiling;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;
using Robust.Shared.Replays;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Threading;
using Robust.Shared.Timing;
using Robust.Shared.Upload;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Robust.Client
{
    internal sealed partial class GameController : IGameControllerInternal
    {
        [Dependency] private readonly INetConfigurationManagerInternal _configurationManager = default!;
        [Dependency] private readonly IResourceCacheInternal _resourceCache = default!;
        [Dependency] private readonly IResourceManagerInternal _resManager = default!;
        [Dependency] private readonly IRobustSerializer _serializer = default!;
        [Dependency] private readonly IXamlProxyManager _xamlProxyManager = default!;
        [Dependency] private readonly IXamlHotReloadManager _xamlHotReloadManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IClientNetManager _networkManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IStateManager _stateManager = default!;
        [Dependency] private readonly IUserInterfaceManagerInternal _userInterfaceManager = default!;
        [Dependency] private readonly IBaseClient _client = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IClientConsoleHost _console = default!;
        [Dependency] private readonly ITimerManager _timerManager = default!;
        [Dependency] private readonly IClientEntityManager _entityManager = default!;
        [Dependency] private readonly IPlacementManager _placementManager = default!;
        [Dependency] private readonly IClientGameStateManager _gameStateManager = default!;
        [Dependency] private readonly IOverlayManagerInternal _overlayManager = default!;
        [Dependency] private readonly ILogManager _logManager = default!;
        [Dependency] private readonly ITaskManager _taskManager = default!;
        [Dependency] private readonly IClientViewVariablesManagerInternal _viewVariablesManager = default!;
        [Dependency] private readonly IDiscordRichPresence _discord = default!;
        [Dependency] private readonly IClydeInternal _clyde = default!;
        [Dependency] private readonly IAudioInternal _audio = default!;
        [Dependency] private readonly IFontManagerInternal _fontManager = default!;
        [Dependency] private readonly IModLoaderInternal _modLoader = default!;
        [Dependency] private readonly IScriptClient _scriptClient = default!;
        [Dependency] private readonly IRobustMappedStringSerializer _stringSerializer = default!;
        [Dependency] private readonly IAuthManager _authManager = default!;
        [Dependency] private readonly IMidiManager _midiManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IParallelManagerInternal _parallelMgr = default!;
        [Dependency] private readonly ProfManager _prof = default!;
        [Dependency] private readonly IRuntimeLog _runtimeLog = default!;
        [Dependency] private readonly ISerializationManager _serializationManager = default!;
        [Dependency] private readonly MarkupTagManager _tagManager = default!;
        [Dependency] private readonly IGamePrototypeLoadManager _protoLoadMan = default!;
        [Dependency] private readonly NetworkResourceManager _netResMan = default!;
        [Dependency] private readonly IReplayLoadManager _replayLoader = default!;
        [Dependency] private readonly IReplayPlaybackManager _replayPlayback = default!;
        [Dependency] private readonly IReplayRecordingManagerInternal _replayRecording = default!;
        [Dependency] private readonly IReflectionManager _reflectionManager = default!;
        [Dependency] private readonly IReloadManager _reload = default!;
        [Dependency] private readonly ILocalizationManager _loc = default!;
        [Dependency] private readonly ISystemFontManagerInternal _systemFontManager = default!;
        [Dependency] private readonly LoadingScreenManager _loadscr = default!;

        private IWebViewManagerHook? _webViewHook;

        private CommandLineArgs? _commandLineArgs;

        // Arguments for loader-load. Not used otherwise.
        private IMainArgs? _loaderArgs;

        public bool ContentStart { get; set; } = false;
        public GameControllerOptions Options { get; private set; } = new();
        public InitialLaunchState LaunchState { get; private set; } = default!;

        private ResourceManifestData? _resourceManifest;

        private DisplayMode _displayMode;

        public void SetCommandLineArgs(CommandLineArgs args)
        {
            _commandLineArgs = args;
        }

        public string GameTitle()
        {
            return Options.DefaultWindowTitle ?? _resourceManifest!.DefaultWindowTitle ?? "RobustToolbox";
        }

        public string WindowIconSet()
        {
            return Options.WindowIconSet?.ToString() ?? _resourceManifest!.WindowIconSet ?? "";
        }

        public string SplashLogo()
        {
            return Options.SplashLogo?.ToString() ?? _resourceManifest!.SplashLogo ?? "";
        }

        public bool ShowLoadingBar()
        {
            return _resourceManifest!.ShowLoadingBar ?? _configurationManager.GetCVar(CVars.LoadingShowBar);
        }

        internal bool StartupContinue(DisplayMode displayMode)
        {
            DebugTools.AssertNotNull(_resourceManifest);

            _loadscr.Initialize(42);

            _loadscr.BeginLoadingSection("Init graphics", dontRender: true);
            _clyde.InitializePostWindowing();
            _clyde.SetWindowTitle(GameTitle());
            _loadscr.EndLoadingSection();

            _loadscr.LoadingStep(_audio.InitializePostWindowing, "Init audio");

            _loadscr.LoadingStep(_taskManager.Initialize, _taskManager);
            _loadscr.LoadingStep(_parallelMgr.Initialize, _parallelMgr);
            _fontManager.SetFontDpi((uint)_configurationManager.GetCVar(CVars.DisplayFontDpi));

            _loadscr.LoadingStep(_systemFontManager.Initialize, "System fonts");

            // Load optional Robust modules.
            _loadscr.LoadingStep(() => LoadOptionalRobustModules(displayMode, _resourceManifest!), "Robust Modules");

            _loadscr.BeginLoadingSection(_modLoader);
            // Disable load context usage on content start.
            // This prevents Content.Client being loaded twice and things like csi blowing up because of it.
            _modLoader.SetUseLoadContext(!ContentStart);
            var disableSandbox = Environment.GetEnvironmentVariable("ROBUST_DISABLE_SANDBOX") == "1";
            _modLoader.SetEnableSandboxing(!disableSandbox && Options.Sandboxing);
            if (!LoadModules())
                return false;

            foreach (var loadedModule in _modLoader.LoadedModules)
            {
                _configurationManager.LoadCVarsFromAssembly(loadedModule);
            }

            _loadscr.EndLoadingSection();

            _loadscr.LoadingStep(_serializationManager.Initialize, _serializationManager);
            _loadscr.LoadingStep(_loc.Initialize, _loc);

            // Call Init in game assemblies.
            _loadscr.LoadingStep(() => _modLoader.BroadcastRunLevel(ModRunLevel.PreInit), "Content PreInit");

            _loadscr.LoadingStep(() =>
                {
                    // Finish initialization of WebView if loaded.
                    if (_webViewHook != null)
                        _loadscr.LoadingStep(_webViewHook.Initialize, _webViewHook);
                },
                "WebView init");

            _loadscr.LoadingStep(() => _modLoader.BroadcastRunLevel(ModRunLevel.Init), "Content Init");

            // Start bad file extensions check after content init,
            // in case content screws with the VFS.
            var checkBadExtensions = ProgramShared.CheckBadFileExtensions(
                _resManager,
                _configurationManager,
                _logManager.GetSawmill("res"));

            _loadscr.LoadingStep(_resourceCache.PreloadTextures, "Texture preload");
            _loadscr.LoadingStep(() => { _networkManager.Initialize(false); }, _networkManager);
            _loadscr.LoadingStep(_configurationManager.SetupNetworking, _configurationManager);
            _loadscr.LoadingStep(_serializer.Initialize, _serializer);
            _loadscr.LoadingStep(_inputManager.Initialize, _inputManager);
            _loadscr.LoadingStep(_console.Initialize, _console);

            // Make sure this is done before we try to load prototypes,
            // avoid any possibility of race conditions causing the check to not finish
            // before prototype load.
            _loadscr.LoadingStep(
                () => ProgramShared.FinishCheckBadFileExtensions(checkBadExtensions),
                "Check bad file extensions");

            _loadscr.LoadingStep(_reload.Initialize, _reload);
            _loadscr.LoadingStep(_reflectionManager.Initialize, _reflectionManager);
            _loadscr.BeginLoadingSection(_prototypeManager);
            _prototypeManager.Initialize();
            _prototypeManager.LoadDefaultPrototypes();
            _loadscr.EndLoadingSection();
            _loadscr.LoadingStep(_xamlProxyManager.Initialize, _xamlProxyManager);
            _loadscr.LoadingStep(_xamlHotReloadManager.Initialize, _xamlHotReloadManager);
            _loadscr.LoadingStep(_userInterfaceManager.Initialize, "UI init");
            _loadscr.LoadingStep(_eyeManager.Initialize, _eyeManager);
            _loadscr.LoadingStep(_entityManager.Initialize, _entityManager);
            _loadscr.LoadingStep(_mapManager.Initialize, _mapManager);
            _loadscr.LoadingStep(_gameStateManager.Initialize, _gameStateManager);
            _loadscr.LoadingStep(_placementManager.Initialize, _placementManager);
            _loadscr.LoadingStep(_viewVariablesManager.Initialize, _viewVariablesManager);
            _loadscr.LoadingStep(_scriptClient.Initialize, _scriptClient);
            _loadscr.LoadingStep(_client.Initialize, _client);
            _loadscr.LoadingStep(_discord.Initialize, _discord);
            _loadscr.LoadingStep(_tagManager.Initialize, _tagManager);
            _loadscr.LoadingStep(_protoLoadMan.Initialize, _protoLoadMan);
            _loadscr.LoadingStep(_netResMan.Initialize, _netResMan);
            _loadscr.LoadingStep(_replayLoader.Initialize, _replayLoader);
            _loadscr.LoadingStep(_replayPlayback.Initialize, _replayPlayback);
            _loadscr.LoadingStep(_replayRecording.Initialize, _replayRecording);
            _loadscr.LoadingStep(_userInterfaceManager.PostInitialize, "UI postinit");

            // Init stuff before this if at all possible.

            _loadscr.LoadingStep(() => _modLoader.BroadcastRunLevel(ModRunLevel.PostInit), "Content PostInit");

            _loadscr.Finish();

            if (_commandLineArgs?.Username != null)
            {
                _client.PlayerNameOverride = _commandLineArgs.Username;
            }

            _authManager.LoadFromEnv();

            if (_configurationManager.GetCVar(CVars.SysGCCollectStart))
            {
                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect();
            }

            // Setup main loop
            if (_mainLoop == null)
            {
                _mainLoop = new GameLoop(
                    _gameTiming,
                    _runtimeLog,
                    _prof,
                    _logManager.GetSawmill("eng"),
                    GameLoopOptions.FromCVars(_configurationManager))
                {
                    SleepMode = displayMode == DisplayMode.Headless ? SleepMode.Delay : SleepMode.None
                };
            }

            _mainLoop.Tick += (sender, args) =>
            {
                if (_mainLoop.Running)
                {
                    Tick(args);
                }
            };

            _mainLoop.Render += (sender, args) =>
            {
                if (_mainLoop.Running)
                {
                    _gameTiming.CurFrame++;
                    _clyde.Render();
                }
            };
            _mainLoop.Input += (sender, args) =>
            {
                if (_mainLoop.Running)
                {
                    Input(args);
                }
            };

            _mainLoop.Update += (sender, args) =>
            {
                if (_mainLoop.Running)
                {
                    Update(args);
                }
            };

            _configurationManager.OnValueChanged(CVars.DisplayMaxFPS, _ => UpdateVsyncConfig());
            _configurationManager.OnValueChanged(CVars.DisplayVSync, _ => UpdateVsyncConfig(), invokeImmediately: true);

            _clyde.Ready();

            if (_resourceManifest!.AutoConnect &&
                (_commandLineArgs?.Connect == true || _commandLineArgs?.Launcher == true)
                && LaunchState.ConnectEndpoint != null)
            {
                _client.ConnectToServer(LaunchState.ConnectEndpoint);
            }

            ProgramShared.RunExecCommands(_console, _commandLineArgs?.ExecCommands);

            return true;
        }

        private bool LoadModules()
        {
            DebugTools.Assert(_resourceManifest != null);

            var assemblyPrefix = Options.ContentModulePrefix ?? _resourceManifest!.AssemblyPrefix ?? "Content.";
            var assemblyDir = Options.AssemblyDirectory;

            bool result;
            if (_resourceManifest.ClientAssemblies is { } clientAssemblies)
            {
                // We have client assemblies. Load only the assemblies listed in the content manifest.
                var paths = clientAssemblies.Select(p => assemblyDir / $"{p}.dll");
                result = _modLoader.TryLoadModules(paths);
            }
            else
            {
                result = _modLoader.TryLoadModulesFrom(assemblyDir, assemblyPrefix);
            }

            if (!result)
            {
                _logger.Fatal("Errors while loading content assemblies.");
                return false;
            }

            return true;
        }

        internal bool StartupSystemSplash(
            GameControllerOptions options,
            Func<ILogHandler>? logHandlerFactory,
            bool globalExceptionLog = false)
        {
            Options = options;
            ReadInitialLaunchState();

            SetupLogging(_logManager, logHandlerFactory ?? (() => new ConsoleLogHandler()), globalExceptionLog);

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

                    _logManager.GetSawmill(sawmill).Level = logLevel;
                }
            }

            ProgramShared.PrintRuntimeInfo(_logManager.RootSawmill);

            // Figure out user data directory.
            var userDataDir = GetUserDataDir();

            _configurationManager.Initialize(false);
            _configurationManager.LoadCVarsFromAssembly(typeof(GameController).Assembly); // Client
            _configurationManager.LoadCVarsFromAssembly(typeof(IConfigurationManager).Assembly); // Shared

            CVarDefaultOverrides.OverrideClient(_configurationManager);

            if (Options.LoadConfigAndUserData)
            {
                var configFile = Path.Combine(userDataDir, Options.ConfigFileName);
                if (File.Exists(configFile))
                {
                    // Load config from user data if available.
                    _configurationManager.LoadFromFile(configFile);
                }
                else
                {
                    // Else we just use code-defined defaults and let it save to file when the user changes things.
                    _configurationManager.SetSaveFile(configFile);
                }
            }

            _configurationManager.OverrideConVars(EnvironmentVariables.GetEnvironmentCVars());

            if (_commandLineArgs != null)
            {
                _configurationManager.OverrideConVars(_commandLineArgs.CVars);
            }

            ProfileOptSetup.Setup(_configurationManager);

            _prof.Initialize();

            _resManager.Initialize(Options.LoadConfigAndUserData ? userDataDir : null, hideUserDataDir: true);

            var mountOptions = _commandLineArgs != null
                ? MountOptions.Merge(_commandLineArgs.MountOptions, Options.MountOptions)
                : Options.MountOptions;

            ProgramShared.DoMounts(_resManager, mountOptions, Options.ContentBuildDirectory,
                Options.AssemblyDirectory,
                Options.LoadContentResources, _loaderArgs != null && !Options.ResourceMountDisabled, ContentStart);

            if (_loaderArgs != null)
            {
                if (_loaderArgs.ApiMounts is { } mounts)
                {
                    foreach (var (api, prefix) in mounts)
                    {
                        _resourceCache.MountLoaderApi(_resManager, api, "", new(prefix));
                    }
                }

                _stringSerializer.EnableCaching = false;
                _resourceCache.MountLoaderApi(_resManager, _loaderArgs.FileApi, "Resources/");
                _modLoader.VerifierExtraLoadHandler = VerifierExtraLoadHandler;
            }

            _resourceManifest = ResourceManifestData.LoadResourceManifest(_resManager);

            {
                // Handle GameControllerOptions implicit CVar overrides.
                _configurationManager.OverrideConVars(new[]
                {
                    (CVars.DisplayWindowIconSet.Name, WindowIconSet()),
                    (CVars.DisplaySplashLogo.Name, SplashLogo()),
                    (CVars.LoadingShowBar.Name, ShowLoadingBar().ToString()),
                });
            }

            _clyde.TextEntered += TextEntered;
            _clyde.TextEditing += TextEditing;
            _clyde.MouseMove += MouseMove;
            _clyde.KeyUp += KeyUp;
            _clyde.KeyDown += KeyDown;
            _clyde.MouseWheel += MouseWheel;
            _clyde.CloseWindow += args =>
            {
                if (args.Window == _clyde.MainWindow)
                {
                    Shutdown("Main window closed");
                }
            };

            // Bring display up as soon as resources are mounted.
            return _clyde.InitializePreWindowing();
        }

        private Stream? VerifierExtraLoadHandler(string arg)
        {
            DebugTools.AssertNotNull(_loaderArgs);

            if (_loaderArgs!.FileApi.TryOpen(arg, out var stream))
            {
                return stream;
            }

            return null;
        }

        private void ReadInitialLaunchState()
        {
            if (_commandLineArgs == null)
            {
                LaunchState = new InitialLaunchState(false, null, null, null);
            }
            else
            {
                var addr = _commandLineArgs.ConnectAddress;
                if (!addr.Contains("://"))
                {
                    addr = "udp://" + addr;
                }

                var uri = new Uri(addr);

                if (uri.Scheme != "udp")
                {
                    _logger.Warning($"connect-address '{uri}' does not have URI scheme of udp://..");
                }

                LaunchState = new InitialLaunchState(
                    _commandLineArgs.Launcher,
                    _commandLineArgs.ConnectAddress,
                    _commandLineArgs.Ss14Address,
                    new DnsEndPoint(uri.Host, uri.IsDefaultPort ? 1212 : uri.Port));
            }
        }

        public void Shutdown(string? reason = null)
        {
            if (_mainLoop == null)
            {
                if (!_dontStart)
                {
                    _logger.Info($"Shutdown called before client init completed: {reason ?? "No reason provided"}");
                    _dontStart = true;
                }
                return;
            }

            // Already got shut down I assume,
            if (!_mainLoop.Running)
            {
                return;
            }

            if (reason != null)
            {
                _logger.Info($"Shutting down! Reason: {reason}");
            }
            else
            {
                _logger.Info("Shutting down!");
            }

            _mainLoop.Running = false;
        }

        private void Input(FrameEventArgs frameEventArgs)
        {
            using (_prof.Group("Input Events"))
            {
                _clyde.ProcessInput(frameEventArgs);
            }

            using (_prof.Group("Network"))
            {
                _networkManager.ProcessPackets();
            }

            using (_prof.Group("Async"))
            {
                _taskManager.ProcessPendingTasks(); // tasks like connect
            }

            using (_prof.Group("Content post engine"))
            {
                _modLoader.BroadcastUpdate(ModUpdateLevel.InputPostEngine, frameEventArgs);
            }
        }

        private void Tick(FrameEventArgs frameEventArgs)
        {
            using (_prof.Group("Content pre engine"))
            {
                _modLoader.BroadcastUpdate(ModUpdateLevel.PreEngine, frameEventArgs);
            }

            using (_prof.Group("Console"))
            {
                _console.CommandBufferExecute();
            }

            using (_prof.Group("Timers"))
            {
                _timerManager.UpdateTimers(frameEventArgs);
            }

            using (_prof.Group("Async"))
            {
                _taskManager.ProcessPendingTasks();
            }

            // GameStateManager is in full control of the simulation update in multiplayer.
            if (_client.RunLevel == ClientRunLevel.InGame || _client.RunLevel == ClientRunLevel.Connected)
            {
                using (_prof.Group("Game state"))
                {
                    _gameStateManager.ApplyGameState();
                }
            }

            // In singleplayer, however, we're in full control instead.
            else if (_client.RunLevel == ClientRunLevel.SinglePlayerGame)
            {
                using (_prof.Group("Entity"))
                {
                    if (TickUpdateOverride != null)
                    {
                        TickUpdateOverride.Invoke(frameEventArgs);
                    }
                    else
                    {
                        // The last real tick is the current tick! This way we won't be in "prediction" mode.
                        _gameTiming.LastRealTick = _gameTiming.LastProcessedTick = _gameTiming.CurTick;
                        _entityManager.TickUpdate(frameEventArgs.DeltaSeconds, noPredictions: false);
                    }
                }
            }

            using (_prof.Group("Content post engine"))
            {
                _modLoader.BroadcastUpdate(ModUpdateLevel.PostEngine, frameEventArgs);
            }
        }

        private void Update(FrameEventArgs frameEventArgs)
        {
            if (_webViewHook != null)
            {
                using (_prof.Group("WebView"))
                {
                    _webViewHook?.Update();
                }
            }

            using (_prof.Group("Clyde"))
            {
                _clyde.FrameProcess(frameEventArgs);
            }

            using (_prof.Group("Content Pre Engine"))
            {
                _modLoader.BroadcastUpdate(ModUpdateLevel.FramePreEngine, frameEventArgs);
            }

            using (_prof.Group("State"))
            {
                _stateManager.FrameUpdate(frameEventArgs);
            }

            if (_client.RunLevel >= ClientRunLevel.Connected)
            {
                using (_prof.Group("Placement"))
                {
                    _placementManager.FrameUpdate(frameEventArgs);
                }

                using (_prof.Group("Entity"))
                {
                    _entityManager.FrameUpdate(frameEventArgs.DeltaSeconds);
                }
            }

            using (_prof.Group("Overlay"))
            {
                _overlayManager.FrameUpdate(frameEventArgs);
            }

            using (_prof.Group("UI"))
            {
                _userInterfaceManager.FrameUpdate(frameEventArgs);
            }

            using (_prof.Group("Content Post Engine"))
            {
                _modLoader.BroadcastUpdate(ModUpdateLevel.FramePostEngine, frameEventArgs);
            }

            _audio.FlushALDisposeQueues();
        }

        internal static void SetupLogging(
            ILogManager logManager,
            Func<ILogHandler> logHandlerFactory,
            bool globalExceptionLog)
        {
            logManager.RootSawmill.AddHandler(logHandlerFactory());

            //logManager.GetSawmill("res.typecheck").Level = LogLevel.Info;
            logManager.GetSawmill("res.tex").Level = LogLevel.Info;
            logManager.GetSawmill("console").Level = LogLevel.Info;
            logManager.GetSawmill("go.sys").Level = LogLevel.Info;
            logManager.GetSawmill("ogl.debug.performance").Level = LogLevel.Fatal;
            // Stupid nvidia driver spams buffer info on DebugTypeOther every time you re-allocate a buffer.
            logManager.GetSawmill("ogl.debug.other").Level = LogLevel.Warning;
            logManager.GetSawmill("gdparse").Level = LogLevel.Error;
            logManager.GetSawmill("discord").Level = LogLevel.Warning;
            logManager.GetSawmill("szr").Level = LogLevel.Info;
            logManager.GetSawmill("loc").Level = LogLevel.Warning;

            if (globalExceptionLog)
            {
#if DEBUG_ONLY_FCE_INFO
#if DEBUG_ONLY_FCE_LOG
                var fce = logManager.GetSawmill("fce");
#endif
                AppDomain.CurrentDomain.FirstChanceException += (sender, args) =>
                {
                    // TODO: record FCE stats
#if DEBUG_ONLY_FCE_LOG
                    fce.Fatal(message);
#endif
                }
#endif

                var uh = logManager.GetSawmill("unhandled");
                AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
                {
                    var message = ((Exception)args.ExceptionObject).ToString();
                    uh.Log(args.IsTerminating ? LogLevel.Fatal : LogLevel.Error, message);
                };

                var uo = logManager.GetSawmill("unobserved");
                TaskScheduler.UnobservedTaskException += (sender, args) =>
                {
                    uo.Error(args.Exception!.ToString());
#if EXCEPTION_TOLERANCE
                    args.SetObserved(); // don't crash
#endif
                };
            }
        }

        private string GetUserDataDir()
        {
            if (_commandLineArgs?.SelfContained == true)
            {
                // Self contained mode. Data is stored in a directory called user_data next to Robust.Client.exe.
                var exeDir = typeof(GameController).Assembly.Location;
                if (string.IsNullOrEmpty(exeDir))
                {
                    throw new Exception("Unable to locate client exe");
                }

                exeDir = Path.GetDirectoryName(exeDir);
                return Path.Combine(exeDir ?? throw new InvalidOperationException(), "user_data");
            }

            return UserDataDir.GetUserDataDir(this);
        }


        private void UpdateVsyncConfig()
        {
            if (_displayMode == DisplayMode.Headless)
                return;

            var vsync = _configurationManager.GetCVar(CVars.DisplayVSync);
            var maxFps = Math.Clamp(_configurationManager.GetCVar(CVars.DisplayMaxFPS), 0, 10_000);

            _clyde.VsyncEnabled = vsync;

            if (_mainLoop == null)
                return;

            if (vsync || maxFps == 0)
            {
                _mainLoop.SleepMode = SleepMode.None;
            }
            else
            {
                _mainLoop.SleepMode = SleepMode.Limit;
                _mainLoop.LimitMinFrameTime = TimeSpan.FromSeconds(1.0 / maxFps);
            }
        }

        internal enum DisplayMode : byte
        {
            Headless,
            Clyde,
        }

        internal void CleanupGameThread()
        {
            _replayRecording.Shutdown();

            _modLoader.Shutdown();

            // CEF specifically makes a massive silent stink of it if we don't shut it down from the correct thread.
            _webViewHook?.Shutdown();

            _networkManager.Shutdown("Client shutting down");
            _midiManager.Shutdown();
            _entityManager.Shutdown();
        }

        internal void CleanupWindowThread()
        {
            _clyde.Shutdown();
            _audio.Shutdown();
        }

        public event Action<FrameEventArgs>? TickUpdateOverride;
    }
}
