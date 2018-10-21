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
using System.Threading;
using JetBrains.Annotations;
using SS14.Client.Utility;
using SS14.Client.ViewVariables;
using SS14.Shared;
using SS14.Shared.Asynchronous;
using SS14.Shared.Interfaces.Resources;

namespace SS14.Client
{
    // Gets automatically ran by SS14.Client.Godot.
    [UsedImplicitly]
    public sealed partial class GameController : IGameController
    {
        internal static bool OnGodot { get; set; }

        /// <summary>
        ///     QueueFreeing a Godot node during finalization can cause segfaults.
        ///     As such, this var is set as soon as we tell Godot to shut down proper.
        /// </summary>
        public static bool ShuttingDownHard { get; private set; } = false;

        [Dependency] private readonly IConfigurationManager _configurationManager;
        [Dependency] private readonly IResourceCache _resourceCache;
        [Dependency] private readonly IResourceManager _resourceManager;
        [Dependency] private readonly ISS14Serializer _serializer;
        [Dependency] private readonly IPrototypeManager _prototypeManager;
        [Dependency] private readonly IClientTileDefinitionManager _tileDefinitionManager;
        [Dependency] private readonly IClientNetManager _networkManager;
        [Dependency] private readonly IMapManager _mapManager;
        [Dependency] private readonly IStateManager _stateManager;
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager;
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
        [Dependency] private readonly IOverlayManager _overlayManager;
        [Dependency] private readonly ILogManager _logManager;
        [Dependency] private readonly ITaskManager _taskManager;

        [Dependency] private readonly IViewVariablesManagerInternal _viewVariablesManager;

        private void Startup()
        {
            InitIoC();
            SetupLogging();

            // Set up custom synchronization context.
            // Sorry Godot.
            _taskManager.Initialize();

            // Load config.
            _configurationManager.LoadFromFile(PathHelpers.ExecutableRelativeFile("client_config.toml"));

            _displayManager.Initialize();
            _displayManager.SetWindowTitle("Space Station 14");

            // Init resources.
            // Doesn't do anything right now because TODO Godot asset management is a bit ad-hoc.
            _resourceCache.LoadBaseResources();
            _resourceCache.LoadLocalResources();

            //identical code for server in baseserver
            if (!AssemblyLoader.TryLoadAssembly<GameShared>(_resourceManager, $"Content.Shared"))
            {
                Logger.Warning($"[ENG] Could not load any Shared DLL.");
            }

            if (!AssemblyLoader.TryLoadAssembly<GameClient>(_resourceManager, $"Content.Client"))
            {
                Logger.Warning($"[ENG] Could not load any Client DLL.");
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
            _tileDefinitionManager.Initialize();
            _mapManager.Initialize();
            _placementManager.Initialize();
            _lightManager.Initialize();
            _entityManager.Initialize();
            _gameStateManager.Initialize();
            _overlayManager.Initialize();
            _viewVariablesManager.Initialize();

            _client.Initialize();

            AssemblyLoader.BroadcastRunLevel(AssemblyLoader.RunLevel.PostInit);

            _stateManager.RequestStateChange<MainScreen>();

            var args = GetCommandLineArgs();
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

            Logger.Debug("Goodbye");
            IoCManager.Clear();
            ShuttingDownHard = true;
            // Hahaha Godot is crashing absurdly and I can't be bothered to fix it.
            // Hey now it shuts down easily.
            Environment.Exit(0);
        }

        private void Update(float frameTime)
        {
            _networkManager.ProcessPackets();
            var eventArgs = new ProcessFrameEventArgs(frameTime);
            AssemblyLoader.BroadcastUpdate(AssemblyLoader.UpdateLevel.PreEngine, eventArgs.Elapsed);
            _timerManager.UpdateTimers(frameTime);
            _taskManager.ProcessPendingTasks();
            _userInterfaceManager.Update(eventArgs);
            _stateManager.Update(eventArgs);
            AssemblyLoader.BroadcastUpdate(AssemblyLoader.UpdateLevel.PostEngine, eventArgs.Elapsed);
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
        }

        public static ICollection<string> GetCommandLineArgs()
        {
            return OnGodot ? Godot.OS.GetCmdlineArgs() : Environment.GetCommandLineArgs();
        }
    }
}
