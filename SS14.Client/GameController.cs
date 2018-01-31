using SS14.Client.GodotGlue;
using SS14.Client.Interfaces;
using SS14.Client.Interfaces.GameObjects;
using SS14.Client.Interfaces.GameStates;
using SS14.Client.Interfaces.Map;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.Interfaces.State;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.ResourceManagement;
using SS14.Client.State.States;
using SS14.Client.UserInterface;
using SS14.Shared.ContentPack;
using SS14.Shared.Interfaces;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Interfaces.Serialization;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Network.Messages;
using SS14.Shared.Prototypes;
using System;
using System.IO;
using SS14.Client.Interfaces.Input;
using SS14.Client.Console;
using SS14.Client.Interfaces.Graphics.Lighting;
using SS14.Client.Interfaces.Graphics;
using SS14.Shared.Interfaces.Timers;
using SS14.Shared.Configuration;

namespace SS14.Client
{
    // Gets automatically ran by SS14.Client.Godot.
    public sealed partial class GameController : ClientEntryPoint, IGameController
    {
        [Dependency]
        readonly IConfigurationManager _configurationManager;
        [Dependency]
        readonly IResourceCache _resourceCache;
        [Dependency]
        readonly ISceneTreeHolder _sceneTreeHolder;
        [Dependency]
        readonly IResourceManager _resourceManager;
        [Dependency]
        readonly ISS14Serializer _serializer;
        [Dependency]
        readonly IPrototypeManager _prototypeManager;
        [Dependency]
        readonly IClientTileDefinitionManager _tileDefinitionManager;
        [Dependency]
        readonly IClientNetManager _networkManager;
        [Dependency]
        readonly IMapManager _mapManager;
        [Dependency]
        readonly IStateManager _stateManager;
        [Dependency]
        readonly IUserInterfaceManager _userInterfaceManager;
        [Dependency]
        readonly IBaseClient _client;
        [Dependency]
        readonly IKeyBindingManager keyBindingManager;
        [Dependency]
        readonly IClientChatConsole _console;
        [Dependency]
        readonly ILightManager lightManager;
        [Dependency]
        readonly IDisplayManager displayManager;
        [Dependency]
        readonly ITimerManager _timerManager;
        [Dependency]
        readonly IClientEntityManager _entityManager;

        public override void Main(Godot.SceneTree tree)
        {
            InitIoC();

            _sceneTreeHolder.Initialize(tree);

            // Load config.
            _configurationManager.LoadFromFile(PathHelpers.ExecutableRelativeFile("client_config.toml"));

            displayManager.Initialize();

            // Init resources.
            // Doesn't do anything right now because TODO Godot asset management is a bit ad-hoc.
            _resourceCache.LoadBaseResources();
            _resourceCache.LoadLocalResources();

            // load the content dlls into the game
            _configurationManager.RegisterCVar("content.dllprefix", "Sandbox", CVar.ARCHIVE);
            var prefix = _configurationManager.GetCVar<string>("content.dllprefix");

            //identical code for server in baseserver
            if (!TryLoadAssembly<GameShared>($"Content.Shared"))
                if (!TryLoadAssembly<GameShared>($"Sandbox.Shared"))
                    Logger.Warning($"[ENG] Could not load any Shared DLL.");

            if (!TryLoadAssembly<GameClient>($"Content.Client"))
                if (!TryLoadAssembly<GameClient>($"Sandbox.Client"))
                    Logger.Warning($"[ENG] Could not load any Client DLL.");

            // Call Init in game assemblies.
            AssemblyLoader.BroadcastRunLevel(AssemblyLoader.RunLevel.Init);

            keyBindingManager.Initialize();
            _serializer.Initialize();
            _userInterfaceManager.Initialize();
            _tileDefinitionManager.Initialize();
            _networkManager.Initialize(false);
            _console.Initialize();
            _prototypeManager.LoadDirectory(@"./Prototypes/");
            _prototypeManager.Resync();
            _mapManager.Initialize();
            //_placementManager.Initialize();
            lightManager.Initialize();
            _entityManager.Initialize();

            _networkManager.RegisterNetMessage<MsgFullState>(MsgFullState.NAME, message => IoCManager.Resolve<IGameStateManager>().HandleFullStateMessage((MsgFullState)message));
            _networkManager.RegisterNetMessage<MsgStateUpdate>(MsgStateUpdate.NAME, message => IoCManager.Resolve<IGameStateManager>().HandleStateUpdateMessage((MsgStateUpdate)message));

            _client.Initialize();

            _stateManager.RequestStateChange<MainScreen>();
        }

        public override void QuitRequest()
        {
            Shutdown("OS quit request");
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
            _sceneTreeHolder.SceneTree.Quit();
        }

        public override void PhysicsProcess(float delta)
        {
            _networkManager.ProcessPackets();
            var eventArgs = new FrameEventArgs(delta);
            AssemblyLoader.BroadcastUpdate(AssemblyLoader.UpdateLevel.PreEngine, eventArgs.Elapsed);
            _timerManager.UpdateTimers(delta);
            _userInterfaceManager?.Update(eventArgs);
            _stateManager?.Update(eventArgs);
            AssemblyLoader.BroadcastUpdate(AssemblyLoader.UpdateLevel.PreEngine, eventArgs.Elapsed);
        }

        public override void FrameProcess(float delta)
        {
            var eventArgs = new FrameEventArgs(delta);
            lightManager.FrameProcess(eventArgs);
        }

        //identical code for server in baseserver
        private bool TryLoadAssembly<T>(string name) where T : GameShared
        {
            // get the assembly from the file system
            if (_resourceManager.TryContentFileRead($@"Assemblies/{name}.dll", out MemoryStream gameDll))
            {
                Logger.Debug($"[SRV] Loading {name} DLL");

                // see if debug info is present
                if (_resourceManager.TryContentFileRead($@"Assemblies/{name}.pdb", out MemoryStream gamePdb))
                {
                    try
                    {
                        // load the assembly into the process, and bootstrap the GameServer entry point.
                        AssemblyLoader.LoadGameAssembly<T>(gameDll.ToArray(), gamePdb.ToArray());
                        return true;
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"[SRV] Exception loading DLL {name}.dll: {e}");
                        return false;
                    }
                }
                else
                {
                    try
                    {
                        // load the assembly into the process, and bootstrap the GameServer entry point.
                        AssemblyLoader.LoadGameAssembly<T>(gameDll.ToArray());
                        return true;
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"[SRV] Exception loading DLL {name}.dll: {e}");
                        return false;
                    }
                }
            }
            else
            {
                Logger.Warning($"[ENG] Could not load {name} DLL.");
                return false;
            }
        }
    }
}
