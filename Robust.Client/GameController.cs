using System;
using System.Globalization;
using System.IO;
using System.Net;
using Robust.Client.Console;
using Robust.Client.Interfaces;
using Robust.Client.Interfaces.GameObjects;
using Robust.Client.Interfaces.GameStates;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Client.Interfaces.Graphics.Overlays;
using Robust.Client.Interfaces.Input;
using Robust.Client.Interfaces.Placement;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.Interfaces.State;
using Robust.Client.Interfaces.UserInterface;
using Robust.Client.Interfaces.Utility;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.Utility;
using Robust.Client.ViewVariables;
using Robust.Shared;
using Robust.Shared.Asynchronous;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.Log;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Resources;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Interfaces.Timers;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Client
{
    internal sealed partial class GameController : IGameControllerInternal
    {
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;
        [Dependency] private readonly IResourceCacheInternal _resourceCache = default!;
        [Dependency] private readonly IResourceManager _resourceManager = default!;
        [Dependency] private readonly IRobustSerializer _serializer = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IClientNetManager _networkManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IStateManager _stateManager = default!;
        [Dependency] private readonly IUserInterfaceManagerInternal _userInterfaceManager = default!;
        [Dependency] private readonly IBaseClient _client = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IClientConsole _console = default!;
        [Dependency] private readonly ITimerManager _timerManager = default!;
        [Dependency] private readonly IClientEntityManager _entityManager = default!;
        [Dependency] private readonly IPlacementManager _placementManager = default!;
        [Dependency] private readonly IClientGameStateManager _gameStateManager = default!;
        [Dependency] private readonly IOverlayManagerInternal _overlayManager = default!;
        [Dependency] private readonly ILogManager _logManager = default!;
        [Dependency] private readonly ITaskManager _taskManager = default!;
        [Dependency] private readonly IViewVariablesManagerInternal _viewVariablesManager = default!;
        [Dependency] private readonly IDiscordRichPresence _discord = default!;
        [Dependency] private readonly IClydeInternal _clyde = default!;
        [Dependency] private readonly IFontManagerInternal _fontManager = default!;
        [Dependency] private readonly IModLoader _modLoader = default!;
        [Dependency] private readonly ISignalHandler _signalHandler = default!;
        [Dependency] private readonly IClientConGroupController _conGroupController = default!;
        [Dependency] private readonly IScriptClient _scriptClient = default!;

        private CommandLineArgs? _commandLineArgs;
        private bool _disableAssemblyLoadContext;

        public InitialLaunchState LaunchState { get; private set; } = default!;

        public bool LoadConfigAndUserData { get; set; } = true;

        public void SetCommandLineArgs(CommandLineArgs args)
        {
            _commandLineArgs = args;
        }

        public bool Startup(Func<ILogHandler>? logHandlerFactory = null)
        {
            ReadInitialLaunchState();

            SetupLogging(_logManager, logHandlerFactory ?? (() => new ConsoleLogHandler()));

            _taskManager.Initialize();

            // Figure out user data directory.
            var userDataDir = GetUserDataDir();

            if (LoadConfigAndUserData)
            {
                var configFile = Path.Combine(userDataDir, "client_config.toml");
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

            _signalHandler.MaybeStart();

            _resourceCache.Initialize(LoadConfigAndUserData ? userDataDir : null);

#if FULL_RELEASE
            _resourceCache.MountContentDirectory(@"Resources/");
#else
            var contentRootDir = ProgramShared.FindContentRootDir();
            _resourceCache.MountContentDirectory($@"{contentRootDir}RobustToolbox/Resources/");
            _resourceCache.MountContentDirectory($@"{contentRootDir}bin/Content.Client/",
                new ResourcePath("/Assemblies/"));
            _resourceCache.MountContentDirectory($@"{contentRootDir}Resources/");
#endif

            // Bring display up as soon as resources are mounted.
            if (!_clyde.Initialize())
            {
                return false;
            }

            _clyde.SetWindowTitle("Space Station 14");

            _fontManager.Initialize();

            // Disable load context usage on content start.
            // This prevents Content.Client being loaded twice and things like csi blowing up because of it.
            _modLoader.SetUseLoadContext(!_disableAssemblyLoadContext);

            //identical code for server in baseserver
            if (!_modLoader.TryLoadAssembly<GameShared>(_resourceManager, $"Content.Shared"))
            {
                Logger.FatalS("eng", "Could not load any Shared DLL.");
                throw new NotSupportedException("Cannot load client without content assembly");
            }

            if (!_modLoader.TryLoadAssembly<GameClient>(_resourceManager, $"Content.Client"))
            {
                Logger.FatalS("eng", "Could not load any Client DLL.");
                throw new NotSupportedException("Cannot load client without content assembly");
            }

            // Call Init in game assemblies.
            _modLoader.BroadcastRunLevel(ModRunLevel.PreInit);
            _modLoader.BroadcastRunLevel(ModRunLevel.Init);

            _userInterfaceManager.Initialize();
            _networkManager.Initialize(false);
            _serializer.Initialize();
            _inputManager.Initialize();
            _console.Initialize();
            _prototypeManager.LoadDirectory(new ResourcePath(@"/Prototypes/"));
            _prototypeManager.Resync();
            _mapManager.Initialize();
            _entityManager.Initialize();
            _gameStateManager.Initialize();
            _placementManager.Initialize();
            _viewVariablesManager.Initialize();
            _conGroupController.Initialize();
            _scriptClient.Initialize();

            _client.Initialize();
            _discord.Initialize();
            _modLoader.BroadcastRunLevel(ModRunLevel.PostInit);

            if (_commandLineArgs?.Username != null)
            {
                _client.PlayerNameOverride = _commandLineArgs.Username;
            }

            _clyde.Ready();

            if ((_commandLineArgs?.Connect == true || _commandLineArgs?.Launcher == true)
                && LaunchState.ConnectEndpoint != null)
            {
                _client.ConnectToServer(LaunchState.ConnectEndpoint);
            }

            _checkOpenGLVersion();
            return true;
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

        private void _checkOpenGLVersion()
        {
            var debugInfo = _clyde.DebugInfo;

            // == null because I'm too lazy to implement a dummy for headless.
            if (debugInfo == null || debugInfo.OpenGLVersion >= debugInfo.MinimumVersion)
            {
                // OpenGL version supported, we're good.
                return;
            }

            var window = new BadOpenGLVersionWindow(debugInfo);
            window.OpenCentered();
        }


        public void Shutdown(string? reason = null)
        {
            // Already got shut down I assume,
            if (!_mainLoop.Running)
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

        private void Update(FrameEventArgs frameEventArgs)
        {
            _networkManager.ProcessPackets();
            _modLoader.BroadcastUpdate(ModUpdateLevel.PreEngine, frameEventArgs);
            _timerManager.UpdateTimers(frameEventArgs);
            _taskManager.ProcessPendingTasks();
            _userInterfaceManager.Update(frameEventArgs);

            if (_client.RunLevel >= ClientRunLevel.Connected)
            {
                _gameStateManager.ApplyGameState();
            }

            _stateManager.Update(frameEventArgs);
            _modLoader.BroadcastUpdate(ModUpdateLevel.PostEngine, frameEventArgs);
        }

        private void _frameProcessMain(FrameEventArgs frameEventArgs)
        {
            _clyde.FrameProcess(frameEventArgs);
            _modLoader.BroadcastUpdate(ModUpdateLevel.FramePreEngine, frameEventArgs);
            _stateManager.FrameUpdate(frameEventArgs);
            _overlayManager.FrameUpdate(frameEventArgs);
            _userInterfaceManager.FrameUpdate(frameEventArgs);
            _modLoader.BroadcastUpdate(ModUpdateLevel.FramePostEngine, frameEventArgs);
        }

        internal static void SetupLogging(ILogManager logManager, Func<ILogHandler> logHandlerFactory)
        {
            logManager.RootSawmill.AddHandler(logHandlerFactory());

            logManager.GetSawmill("res.typecheck").Level = LogLevel.Info;
            logManager.GetSawmill("res.tex").Level = LogLevel.Info;
            logManager.GetSawmill("console").Level = LogLevel.Info;
            logManager.GetSawmill("go.sys").Level = LogLevel.Info;
            logManager.GetSawmill("ogl.debug.performance").Level = LogLevel.Fatal;
            // Stupid nvidia driver spams buffer info on DebugTypeOther every time you re-allocate a buffer.
            logManager.GetSawmill("ogl.debug.other").Level = LogLevel.Warning;
            logManager.GetSawmill("gdparse").Level = LogLevel.Error;
            logManager.GetSawmill("discord").Level = LogLevel.Warning;
            logManager.GetSawmill("net.predict").Level = LogLevel.Info;
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


        internal enum DisplayMode
        {
            Headless,
            Clyde,
        }
    }
}
