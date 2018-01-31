using SS14.Client.Console;
using SS14.Client.Debugging;
using SS14.Client.GameObjects;
using SS14.Client.GameStates;
using SS14.Client.Graphics.Lighting;
using SS14.Client.Input;
using SS14.Client.Interfaces;
using SS14.Client.Interfaces.Debugging;
using SS14.Client.Interfaces.GameObjects;
using SS14.Client.Interfaces.GameStates;
using SS14.Client.Interfaces.Graphics.Lighting;
using SS14.Client.Interfaces.Input;
using SS14.Client.Interfaces.Map;
using SS14.Client.Interfaces.Player;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.Interfaces.State;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.Log;
using SS14.Client.Map;
using SS14.Client.Player;
using SS14.Client.Reflection;
using SS14.Client.ResourceManagement;
using SS14.Client.State;
using SS14.Client.UserInterface;
using SS14.Shared.Configuration;
using SS14.Shared.ContentPack;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Log;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Interfaces.Physics;
using SS14.Shared.Interfaces.Reflection;
using SS14.Shared.Interfaces.Serialization;
using SS14.Shared.Interfaces.Timing;
using SS14.Shared.Interfaces.Timers;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Network;
using SS14.Shared.Physics;
using SS14.Shared.Prototypes;
using SS14.Shared.Serialization;
using SS14.Shared.Timing;
using SS14.Shared.Timers;
using System;
using System.Collections.Generic;
using System.Reflection;
using SS14.Client.Interfaces.Graphics;
using SS14.Client.Graphics;

namespace SS14.Client
{
    // Partial of GameController to initialize IoC and some other low-level systems like it.
    public sealed partial class GameController
    {
        private void InitIoC()
        {
            RegisterIoC();
            RegisterReflection();
            Logger.Debug("IoC Initialized!");

            // We are not IoC-managed (SS14.Client.Godot spawns us), but we still want the dependencies.
            IoCManager.InjectDependencies(this);

            var proxy = (GameControllerProxy)IoCManager.Resolve<IGameControllerProxy>();
            proxy.GameController = this;
        }

        private static void RegisterIoC()
        {
            // Shared stuff.
            IoCManager.Register<ILogManager, GodotLogManager>();
            IoCManager.Register<IConfigurationManager, ConfigurationManager>();
            IoCManager.Register<IResourceManager, ResourceManager>();
            IoCManager.Register<ISS14Serializer, SS14Serializer>();
            IoCManager.Register<IPrototypeManager, PrototypeManager>();
            IoCManager.Register<ITileDefinitionManager, ClientTileDefinitionManager>();
            IoCManager.Register<INetManager, NetManager>();
            IoCManager.Register<IEntitySystemManager, EntitySystemManager>();
            IoCManager.Register<IEntityManager, ClientEntityManager>();
            IoCManager.Register<IComponentFactory, ClientComponentFactory>();
            IoCManager.Register<IComponentManager, ComponentManager>();
            IoCManager.Register<IMapManager, ClientMapManager>();
            IoCManager.Register<ICollisionManager, CollisionManager>();
            IoCManager.Register<ITimerManager, TimerManager>();

            // Client stuff.
            IoCManager.Register<IReflectionManager, ClientReflectionManager>();
            IoCManager.Register<IResourceCache, ResourceCache>();
            IoCManager.Register<ISceneTreeHolder, SceneTreeHolder>();
            IoCManager.Register<IClientTileDefinitionManager, ClientTileDefinitionManager>();
            IoCManager.Register<IClientNetManager, NetManager>();
            IoCManager.Register<IClientEntityManager, ClientEntityManager>();
            IoCManager.Register<IEntityNetworkManager, ClientEntityNetworkManager>();
            IoCManager.Register<IGameStateManager, GameStateManager>();
            IoCManager.Register<IBaseClient, BaseClient>();
            IoCManager.Register<IPlayerManager, PlayerManager>();
            IoCManager.Register<IStateManager, StateManager>();
            IoCManager.Register<IUserInterfaceManager, UserInterfaceManager>();
            IoCManager.Register<IGameControllerProxy, GameControllerProxy>();
            IoCManager.Register<IKeyBindingManager, KeyBindingManager>();
            IoCManager.Register<IDebugDrawing, DebugDrawing>();
            IoCManager.Register<IClientConsole, ClientChatConsole>();
            IoCManager.Register<IClientChatConsole, ClientChatConsole>();
            IoCManager.Register<ILightManager, LightManager>();
            IoCManager.Register<IDisplayManager, DisplayManager>();

            IoCManager.BuildGraph();
        }

        private static void RegisterReflection()
        {
            // Gets a handle to the shared and the current (client) dll.
            IoCManager.Resolve<IReflectionManager>().LoadAssemblies(new List<Assembly>(2)
            {
                // Do NOT register SS14.Client.Godot.
                // At least not for now.
                AppDomain.CurrentDomain.GetAssemblyByName("SS14.Shared"),
                Assembly.GetExecutingAssembly()
            });
        }
    }
}
