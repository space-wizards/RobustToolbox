using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Robust.Client.Audio.Midi;
using Robust.Client.Console;
using Robust.Client.GameObjects;
using Robust.Client.GameStates;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Placement;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Client.Utility;
using Robust.Client.ViewVariables;
using Robust.LoaderApi;
using Robust.Shared;
using Robust.Shared.Asynchronous;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Client
{
    internal sealed partial class GameController : IGameControllerInternal
    {
        [Dependency] private readonly IConfigurationManagerInternal _configurationManager = default!;
        [Dependency] private readonly IResourceCacheInternal _resourceCache = default!;
        [Dependency] private readonly IRobustSerializer _serializer = default!;
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
        [Dependency] private readonly IEntityLookup _lookup = default!;
        [Dependency] private readonly IPlacementManager _placementManager = default!;
        [Dependency] private readonly IClientGameStateManager _gameStateManager = default!;
        [Dependency] private readonly IOverlayManagerInternal _overlayManager = default!;
        [Dependency] private readonly ILogManager _logManager = default!;
        [Dependency] private readonly ITaskManager _taskManager = default!;
        [Dependency] private readonly IViewVariablesManagerInternal _viewVariablesManager = default!;
        [Dependency] private readonly IDiscordRichPresence _discord = default!;
        [Dependency] private readonly IClydeInternal _clyde = default!;
        [Dependency] private readonly IFontManagerInternal _fontManager = default!;
        [Dependency] private readonly IModLoaderInternal _modLoader = default!;
        [Dependency] private readonly IScriptClient _scriptClient = default!;
        [Dependency] private readonly IRobustMappedStringSerializer _stringSerializer = default!;
        [Dependency] private readonly IAuthManager _authManager = default!;
        [Dependency] private readonly IMidiManager _midiManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;

        private CommandLineArgs? _commandLineArgs;

        // Arguments for loader-load. Not used otherwise.
        private IMainArgs? _loaderArgs;

        public bool ContentStart { get; set; } = false;
        public GameControllerOptions Options { get; private set; } = new();
        public InitialLaunchState LaunchState { get; private set; } = default!;

        public bool LoadConfigAndUserData { get; set; } = true;

        public void SetCommandLineArgs(CommandLineArgs args)
        {
            _commandLineArgs = args;
        }

        private bool StartupContinue(DisplayMode displayMode)
        {
            _clyde.InitializePostWindowing();
            _clyde.SetWindowTitle(Options.DefaultWindowTitle);

            _taskManager.Initialize();
            _fontManager.SetFontDpi((uint) _configurationManager.GetCVar(CVars.DisplayFontDpi));

            // Disable load context usage on content start.
            // This prevents Content.Client being loaded twice and things like csi blowing up because of it.
            _modLoader.SetUseLoadContext(!ContentStart);
            _modLoader.SetEnableSandboxing(Options.Sandboxing);

            if (!_modLoader.TryLoadModulesFrom(new ResourcePath("/Assemblies/"), Options.ContentModulePrefix))
            {
                Logger.Fatal("Errors while loading content assemblies.");
                return false;
            }

            foreach (var loadedModule in _modLoader.LoadedModules)
            {
                _configurationManager.LoadCVarsFromAssembly(loadedModule);
            }

            IoCManager.Resolve<ISerializationManager>().Initialize();

            // Call Init in game assemblies.
            _modLoader.BroadcastRunLevel(ModRunLevel.PreInit);
            _modLoader.BroadcastRunLevel(ModRunLevel.Init);

            _resourceCache.PreloadTextures();
            _userInterfaceManager.Initialize();
            _eyeManager.Initialize();
            _networkManager.Initialize(false);
            IoCManager.Resolve<INetConfigurationManager>().SetupNetworking();
            _serializer.Initialize();
            _inputManager.Initialize();
            _console.Initialize();
            _prototypeManager.Initialize();
            _prototypeManager.LoadDirectory(Options.PrototypeDirectory);
            _prototypeManager.Resync();
            _mapManager.Initialize();
            _entityManager.Initialize();
            _gameStateManager.Initialize();
            _placementManager.Initialize();
            _viewVariablesManager.Initialize();
            _scriptClient.Initialize();

            _client.Initialize();
            _discord.Initialize();
            _modLoader.BroadcastRunLevel(ModRunLevel.PostInit);

            if (_commandLineArgs?.Username != null)
            {
                _client.PlayerNameOverride = _commandLineArgs.Username;
            }

            _authManager.LoadFromEnv();

            GC.Collect();

            // Setup main loop
            if (_mainLoop == null)
            {
                _mainLoop = new GameLoop(_gameTiming)
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

            _clyde.Ready();

            if (!Options.DisableCommandLineConnect &&
                (_commandLineArgs?.Connect == true || _commandLineArgs?.Launcher == true)
                && LaunchState.ConnectEndpoint != null)
            {
                _client.ConnectToServer(LaunchState.ConnectEndpoint);
            }

            return true;
        }

        private bool StartupSystemSplash(Func<ILogHandler>? logHandlerFactory)
        {
            ReadInitialLaunchState();

            SetupLogging(_logManager, logHandlerFactory ?? (() => new ConsoleLogHandler()));

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

            // Figure out user data directory.
            var userDataDir = GetUserDataDir();

            _configurationManager.Initialize(false);

            // MUST load cvars before loading from config file so the cfg manager is aware of secure cvars.
            // So SECURE CVars are blacklisted from config.
            _configurationManager.LoadCVarsFromAssembly(typeof(GameController).Assembly); // Client
            _configurationManager.LoadCVarsFromAssembly(typeof(IConfigurationManager).Assembly); // Shared

            if (LoadConfigAndUserData)
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

            _resourceCache.Initialize(LoadConfigAndUserData ? userDataDir : null);

            var mountOptions = _commandLineArgs != null
                ? MountOptions.Merge(_commandLineArgs.MountOptions, Options.MountOptions) : Options.MountOptions;

            ProgramShared.DoMounts(_resourceCache, mountOptions, Options.ContentBuildDirectory,
                _loaderArgs != null && !Options.ResourceMountDisabled, ContentStart);

            if (_loaderArgs != null)
            {
                _stringSerializer.EnableCaching = false;
                _resourceCache.MountLoaderApi(_loaderArgs.FileApi, "Resources/");
                _modLoader.VerifierExtraLoadHandler = VerifierExtraLoadHandler;
            }

            _clyde.TextEntered += TextEntered;
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
                    Logger.Warning($"connect-address '{uri}' does not have URI scheme of udp://..");
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
            DebugTools.AssertNotNull(_mainLoop);

            // Already got shut down I assume,
            if (!_mainLoop!.Running)
            {
                return;
            }

            if (reason != null)
            {
                Logger.Info($"Shutting down! Reason: {reason}");
            }
            else
            {
                Logger.Info("Shutting down!");
            }

            _mainLoop.Running = false;
        }

        private void Input(FrameEventArgs frameEventArgs)
        {
            _clyde.ProcessInput(frameEventArgs);
            _networkManager.ProcessPackets();
            _taskManager.ProcessPendingTasks(); // tasks like connect
        }

        private void Tick(FrameEventArgs frameEventArgs)
        {
            _modLoader.BroadcastUpdate(ModUpdateLevel.PreEngine, frameEventArgs);
            _timerManager.UpdateTimers(frameEventArgs);
            _taskManager.ProcessPendingTasks();

            // GameStateManager is in full control of the simulation update in multiplayer.
            if (_client.RunLevel == ClientRunLevel.InGame || _client.RunLevel == ClientRunLevel.Connected)
            {
                _gameStateManager.ApplyGameState();
            }

            // In singleplayer, however, we're in full control instead.
            else if (_client.RunLevel == ClientRunLevel.SinglePlayerGame)
            {
                _entityManager.TickUpdate(frameEventArgs.DeltaSeconds);
                _lookup.Update();
            }

            _modLoader.BroadcastUpdate(ModUpdateLevel.PostEngine, frameEventArgs);
        }

        private void Update(FrameEventArgs frameEventArgs)
        {
            _clyde.FrameProcess(frameEventArgs);
            _modLoader.BroadcastUpdate(ModUpdateLevel.FramePreEngine, frameEventArgs);
            _stateManager.FrameUpdate(frameEventArgs);

            if (_client.RunLevel >= ClientRunLevel.Connected)
            {
                _placementManager.FrameUpdate(frameEventArgs);
                _entityManager.FrameUpdate(frameEventArgs.DeltaSeconds);
            }

            _overlayManager.FrameUpdate(frameEventArgs);
            _userInterfaceManager.FrameUpdate(frameEventArgs);
            _modLoader.BroadcastUpdate(ModUpdateLevel.FramePostEngine, frameEventArgs);
        }

        internal static void SetupLogging(ILogManager logManager, Func<ILogHandler> logHandlerFactory)
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
            logManager.GetSawmill("net.predict").Level = LogLevel.Info;
            logManager.GetSawmill("szr").Level = LogLevel.Info;
            logManager.GetSawmill("loc").Level = LogLevel.Error;

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
                var message = ((Exception) args.ExceptionObject).ToString();
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

            return UserDataDir.GetUserDataDir();
        }


        internal enum DisplayMode : byte
        {
            Headless,
            Clyde,
        }

        private void Cleanup()
        {
            _networkManager.Shutdown("Client shutting down");
            _midiManager.Shutdown();
            IoCManager.Resolve<IEntityLookup>().Shutdown();
            _entityManager.Shutdown();
            _clyde.Shutdown();
        }
    }
}
