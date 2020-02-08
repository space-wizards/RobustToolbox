using System;
using System.Globalization;
using System.IO;
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
using Robust.Client.State.States;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.Utility;
using Robust.Client.ViewVariables;
using Robust.Shared;
using Robust.Shared.Asynchronous;
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
#pragma warning disable 649
        [Dependency] private readonly IConfigurationManager _configurationManager;
        [Dependency] private readonly IResourceCacheInternal _resourceCache;
        [Dependency] private readonly IResourceManager _resourceManager;
        [Dependency] private readonly IRobustSerializer _serializer;
        [Dependency] private readonly IPrototypeManager _prototypeManager;
        [Dependency] private readonly IClientNetManager _networkManager;
        [Dependency] private readonly IMapManager _mapManager;
        [Dependency] private readonly IStateManager _stateManager;
        [Dependency] private readonly IUserInterfaceManagerInternal _userInterfaceManager;
        [Dependency] private readonly IBaseClient _client;
        [Dependency] private readonly IInputManager _inputManager;
        [Dependency] private readonly IClientConsole _console;
        [Dependency] private readonly ITimerManager _timerManager;
        [Dependency] private readonly IClientEntityManager _entityManager;
        [Dependency] private readonly IEyeManager _eyeManager;
        [Dependency] private readonly IPlacementManager _placementManager;
        [Dependency] private readonly IClientGameStateManager _gameStateManager;
        [Dependency] private readonly IOverlayManagerInternal _overlayManager;
        [Dependency] private readonly ILogManager _logManager;
        [Dependency] private readonly ITaskManager _taskManager;
        [Dependency] private readonly IViewVariablesManagerInternal _viewVariablesManager;
        [Dependency] private readonly IDiscordRichPresence _discord;
        [Dependency] private readonly IClydeInternal _clyde;
        [Dependency] private readonly IFontManagerInternal _fontManager;
        [Dependency] private readonly ILocalizationManager _localizationManager;
        [Dependency] private readonly IModLoader _modLoader;
        [Dependency] private readonly ISignalHandler _signalHandler;
        [Dependency] private readonly IClientConGroupController _conGroupController;
#pragma warning restore 649

        private CommandLineArgs _commandLineArgs;

        public bool LoadConfigAndUserData { get; set; } = true;

        public void SetCommandLineArgs(CommandLineArgs args)
        {
            _commandLineArgs = args;
        }

        public bool Startup()
        {
            SetupLogging(_logManager);

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

            // Default to en-US.
            // Perhaps in the future we could make a command line arg or something to change this default.
            _localizationManager.LoadCulture(new CultureInfo("en-US"));

            // Bring display up as soon as resources are mounted.
            if (!_clyde.Initialize())
            {
                return false;
            }

            _clyde.SetWindowTitle("Space Station 14");

            _fontManager.Initialize();

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
            _modLoader.BroadcastRunLevel(ModRunLevel.Init);

            _eyeManager.Initialize();
            _serializer.Initialize();
            _userInterfaceManager.Initialize();
            _networkManager.Initialize(false);
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

            _client.Initialize();
            _discord.Initialize();
            _modLoader.BroadcastRunLevel(ModRunLevel.PostInit);

            if (_commandLineArgs?.Launcher == true)
            {
                _stateManager.RequestStateChange<LauncherConnecting>();
            }
            else
            {
                _stateManager.RequestStateChange<MainScreen>();
            }

            if (_commandLineArgs?.Username != null)
            {
                _client.PlayerNameOverride = _commandLineArgs.Username;
            }

            _clyde.Ready();

            if (_commandLineArgs?.Connect == true || _commandLineArgs?.Launcher == true)
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

                _client.ConnectToServer(uri.Host, (ushort) (uri.IsDefaultPort ? 1212 : uri.Port));
            }

            _checkOpenGLVersion();
            return true;
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

        public void Shutdown(string reason = null)
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
            _stateManager.Update(frameEventArgs);

            if (_client.RunLevel >= ClientRunLevel.Connected)
            {
                _gameStateManager.ApplyGameState();
            }

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

        internal static void SetupLogging(ILogManager logManager)
        {
            logManager.RootSawmill.AddHandler(new ConsoleLogHandler());

            logManager.GetSawmill("res.typecheck").Level = LogLevel.Info;
            logManager.GetSawmill("res.tex").Level = LogLevel.Info;
            logManager.GetSawmill("console").Level = LogLevel.Info;
            logManager.GetSawmill("go.sys").Level = LogLevel.Info;
            logManager.GetSawmill("ogl.debug.performance").Level = LogLevel.Fatal;
            // Stupid nvidia driver spams buffer info on DebugTypeOther every time you re-allocate a buffer.
            logManager.GetSawmill("ogl.debug.other").Level = LogLevel.Warning;
            logManager.GetSawmill("gdparse").Level = LogLevel.Error;
            logManager.GetSawmill("discord").Level = LogLevel.Warning;
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
