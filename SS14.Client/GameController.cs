using SS14.Client.Console;
using SS14.Client.Interfaces;
using SS14.Client.Interfaces.GameObjects;
using SS14.Client.Interfaces.GameStates;
using SS14.Client.Interfaces.Graphics;
using SS14.Client.Interfaces.Graphics.ClientEye;
using SS14.Client.Interfaces.Graphics.Lighting;
using SS14.Client.Interfaces.Graphics.Overlays;
using SS14.Client.Interfaces.Input;
using SS14.Client.Interfaces.Map;
using SS14.Client.Interfaces.Placement;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.Interfaces.State;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.Interfaces.Utility;
using SS14.Client.Log;
using SS14.Client.State.States;
using SS14.Shared.ContentPack;
using SS14.Shared.Input;
using SS14.Shared.Interfaces;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Log;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Interfaces.Serialization;
using SS14.Shared.Interfaces.Timers;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Network.Messages;
using SS14.Shared.Prototypes;
using SS14.Shared.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Threading;
using JetBrains.Annotations;
using SS14.Client.ResourceManagement;
using SS14.Client.Utility;
using SS14.Client.ViewVariables;
using SS14.Shared;
using SS14.Shared.Asynchronous;
using SS14.Shared.Interfaces.Resources;

namespace SS14.Client
{
    // Gets automatically ran by SS14.Client.Godot.
    [UsedImplicitly]
    internal sealed partial class GameController : IGameControllerInternal
    {
        public enum DisplayMode
        {
            Headless,
            Godot,
            Clyde
        }

        internal static DisplayMode Mode { get; private set; } = DisplayMode.Headless;

        internal static bool OnGodot => Mode == DisplayMode.Godot;

        /// <summary>
        ///     QueueFreeing a Godot node during finalization can cause segfaults.
        ///     As such, this var is set as soon as we tell Godot to shut down proper.
        /// </summary>
        public static bool ShuttingDownHard { get; private set; } = false;

        [Dependency] private readonly IConfigurationManager _configurationManager;
        [Dependency] private readonly IResourceCacheInternal _resourceCache;
        [Dependency] private readonly IResourceManager _resourceManager;
        [Dependency] private readonly ISS14Serializer _serializer;
        [Dependency] private readonly IPrototypeManager _prototypeManager;
        [Dependency] private readonly IClientNetManager _networkManager;
        [Dependency] private readonly IMapManager _mapManager;
        [Dependency] private readonly IStateManager _stateManager;
        [Dependency] private readonly IUserInterfaceManagerInternal _userInterfaceManager;
        [Dependency] private readonly IBaseClient _client;
        [Dependency] private readonly IInputManager _inputManager;
        [Dependency] private readonly IClientChatConsole _console;
        [Dependency] private readonly ILightManager _lightManager;
        [Dependency] private readonly IDisplayManager _displayManager;
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
        private IClyde _clyde;
        private IFontManagerInternal _fontManager;
        

        private void Startup()
        {
            ThreadUtility.MainThread = Thread.CurrentThread;
            InitIoC();
            SetupLogging();

            var args = GetCommandLineArgs();

            // Set up custom synchronization context.
            // Sorry Godot.
            _taskManager.Initialize();

            // Figure out user data directory.
            var userDataDir = _getUserDataDir(args);

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

            if (Mode == DisplayMode.Clyde)
            {
                _clyde = IoCManager.Resolve<IClyde>();
            }

            _resourceCache.Initialize(userDataDir);
            _resourceCache.LoadBaseResources();

            // Bring display up as soon as resources are mounted.
            _displayManager.Initialize();
            _displayManager.SetWindowTitle("Space Station 14");

            if (Mode == DisplayMode.Clyde)
            {
                _fontManager = IoCManager.Resolve<IFontManagerInternal>();
                _fontManager.Initialize();
            }

            //identical code for server in baseserver
            if (!AssemblyLoader.TryLoadAssembly<GameShared>(_resourceManager, $"Content.Shared"))
            {
                Logger.FatalS("eng", "Could not load any Shared DLL.");
                throw new NotSupportedException("Cannot load client without content assembly");
            }

            if (!AssemblyLoader.TryLoadAssembly<GameClient>(_resourceManager, $"Content.Client"))
            {
                Logger.FatalS("eng", "Could not load any Client DLL.");
                throw new NotSupportedException("Cannot load client without content assembly");
            }

            // Call Init in game assemblies.
            AssemblyLoader.BroadcastRunLevel(AssemblyLoader.RunLevel.Init);

            _eyeManager.Initialize();
            _serializer.Initialize();
            _userInterfaceManager.Initialize();
            _networkManager.Initialize(false);
            _inputManager.Initialize();
            _console.Initialize();
            _prototypeManager.LoadDirectory(new ResourcePath(@"/Prototypes/"));
            _prototypeManager.Resync();
            _mapManager.Initialize();
            _lightManager.Initialize();
            _entityManager.Initialize();
            _gameStateManager.Initialize();
            _overlayManager.Initialize();
            _placementManager.Initialize();
            _viewVariablesManager.Initialize();

            _client.Initialize();
            _discord.Connect();
            AssemblyLoader.BroadcastRunLevel(AssemblyLoader.RunLevel.PostInit);

            _stateManager.RequestStateChange<MainScreen>();

            _clyde?.Ready();

            if (args.Contains("--connect"))
            {
                _client.ConnectToServer("127.0.0.1", 1212);
            }
        }

        public void Shutdown(string reason = null)
        {
            if (reason != null)
            {
                Logger.Info($"Shutting down! Reason: {reason}");
            }
            else
            {
                Logger.Info("Shutting down!");
            }

            if (Mode != DisplayMode.Godot)
            {
                _mainLoop.Running = false;
            }

            Logger.Debug("Goodbye");
            IoCManager.Clear();
            ShuttingDownHard = true;
            // Hahaha Godot is crashing absurdly and I can't be bothered to fix it.
            // Hey now it shuts down easily.
            Environment.Exit(0);
        }

        private void Update(float frameTime)
        {
            var eventArgs = new ProcessFrameEventArgs(frameTime);
            _networkManager.ProcessPackets();
            AssemblyLoader.BroadcastUpdate(AssemblyLoader.UpdateLevel.PreEngine, eventArgs.Elapsed);
            _timerManager.UpdateTimers(frameTime);
            _taskManager.ProcessPendingTasks();
            _userInterfaceManager.Update(eventArgs);
            _stateManager.Update(eventArgs);

            if (_client.RunLevel >= ClientRunLevel.Connected)
            {
                _gameStateManager.ApplyGameState();
            }

            AssemblyLoader.BroadcastUpdate(AssemblyLoader.UpdateLevel.PostEngine, eventArgs.Elapsed);
        }

        private void _frameProcessMain(float delta)
        {
            var eventArgs = new RenderFrameEventArgs(delta);
            _clyde?.FrameProcess(eventArgs);
            AssemblyLoader.BroadcastUpdate(AssemblyLoader.UpdateLevel.FramePreEngine, eventArgs.Elapsed);
            _lightManager.FrameUpdate(eventArgs);
            _stateManager.FrameUpdate(eventArgs);
            _overlayManager.FrameUpdate(eventArgs);
            _userInterfaceManager.FrameUpdate(eventArgs);
            AssemblyLoader.BroadcastUpdate(AssemblyLoader.UpdateLevel.FramePostEngine, eventArgs.Elapsed);
        }

        private void SetupLogging()
        {
            if (OnGodot)
            {
                _logManager.RootSawmill.AddHandler(new GodotLogHandler());
            }
            else
            {
                _logManager.RootSawmill.AddHandler(new ConsoleLogHandler());
            }

            _logManager.GetSawmill("res.typecheck").Level = LogLevel.Info;
            _logManager.GetSawmill("res.tex").Level = LogLevel.Info;
            _logManager.GetSawmill("console").Level = LogLevel.Info;
            _logManager.GetSawmill("go.sys").Level = LogLevel.Info;
            _logManager.GetSawmill("ogl.debug.performance").Level = LogLevel.Fatal;
            // Stupid nvidia driver spams buffer info on DebugTypeOther every time you re-allocate a buffer.
            _logManager.GetSawmill("ogl.debug.other").Level = LogLevel.Warning;
            _logManager.GetSawmill("gdparse").Level = LogLevel.Error;
        }

        public static ICollection<string> GetCommandLineArgs()
        {
            return OnGodot ? Godot.OS.GetCmdlineArgs() : Environment.GetCommandLineArgs();
        }

        private static string _getUserDataDir(ICollection<string> commandLineArgs)
        {
            if (commandLineArgs.Contains("--self-contained"))
            {
                // Self contained mode. Data is stored in a directory called user_data next to SS14.Client.exe.
                var exeDir = Assembly.GetExecutingAssembly().Location;
                if (string.IsNullOrEmpty(exeDir))
                {
                    throw new Exception("Unable to locate client exe");
                }

                exeDir = Path.GetDirectoryName(exeDir);
                return Path.Combine(exeDir ?? throw new InvalidOperationException(), "user_data");
            }

            string appDataDir;

#if LINUX
            var xdgDataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
            if (xdgDataHome == null)
            {
                appDataDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");
            }
            else
            {
                appDataDir = xdgDataHome;
            }
#elif MACOS
            appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                                      "Library", "Application Support");
#else
            appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
#endif

            return Path.Combine(appDataDir, "Space Station 14");
        }
    }
}
