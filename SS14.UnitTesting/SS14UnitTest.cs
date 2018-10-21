using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SS14.Client;
using SS14.Client.Console;
using SS14.Client.Debugging;
using SS14.Client.GameObjects;
using SS14.Client.GameStates;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Overlays;
using SS14.Client.Input;
using SS14.Client.Interfaces;
using SS14.Client.Interfaces.Debugging;
using SS14.Client.Interfaces.GameObjects;
using SS14.Client.Interfaces.GameStates;
using SS14.Client.Interfaces.Graphics;
using SS14.Client.Interfaces.Graphics.Overlays;
using SS14.Client.Interfaces.Input;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.Interfaces.State;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.Reflection;
using SS14.Client.ResourceManagement;
using SS14.Client.State;
using SS14.Client.ViewVariables;
using SS14.Server;
using SS14.Server.Chat;
using SS14.Server.Console;
using SS14.Server.GameObjects;
using SS14.Server.GameStates;
using SS14.Server.Interfaces;
using SS14.Server.Interfaces.Chat;
using SS14.Server.Interfaces.Console;
using SS14.Server.Interfaces.GameObjects;
using SS14.Server.Interfaces.GameState;
using SS14.Server.Interfaces.Maps;
using SS14.Server.Interfaces.Placement;
using SS14.Server.Interfaces.Player;
using SS14.Server.Maps;
using SS14.Server.Placement;
using SS14.Server.Player;
using SS14.Server.Prototypes;
using SS14.Server.Reflection;
using SS14.Server.ViewVariables;
using SS14.Shared.Asynchronous;
using SS14.Shared.Configuration;
using SS14.Shared.ContentPack;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Log;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Interfaces.Physics;
using SS14.Shared.Interfaces.Reflection;
using SS14.Shared.Interfaces.Resources;
using SS14.Shared.Interfaces.Serialization;
using SS14.Shared.Interfaces.Timers;
using SS14.Shared.Interfaces.Timing;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Map;
using SS14.Shared.Network;
using SS14.Shared.Physics;
using SS14.Shared.Prototypes;
using SS14.Shared.Serialization;
using SS14.Shared.Timers;
using SS14.Shared.Timing;
using SS14.UnitTesting.Client;

namespace SS14.UnitTesting
{
    public enum UnitTestProject
    {
        Server,
        Client
    }

    public abstract class SS14UnitTest
    {
        #region Options

        // TODO: make this figured out at runtime so we don't have to pass a compiler flag.
#if HEADLESS
        public const bool Headless = true;
#else
        public const bool Headless = false;
#endif

        // These properties are meant to be overriden to disable certain parts
        // Like loading resource packs, which isn't always needed.

        /// <summary>
        /// Whether the client resource pack should be loaded or not.
        /// </summary>
        public virtual bool NeedsResourcePack => false;

        /// <summary>
        /// Whether the client config should be loaded or not.
        /// </summary>
        public virtual bool NeedsClientConfig => false;

        public virtual UnitTestProject Project => UnitTestProject.Server;

        #endregion Options

        #region Accessors

        public IConfigurationManager GetConfigurationManager { get; private set; }

        #endregion Accessors

        [OneTimeSetUp]
        public void BaseSetup()
        {
            TestFixtureAttribute a = Attribute.GetCustomAttribute(GetType(), typeof(TestFixtureAttribute)) as TestFixtureAttribute;
            if (NeedsResourcePack && Headless)
            {
                // Disable the test automatically.
                a.Explicit = true;
                return;
            }

            // Clear state across tests.
            IoCManager.Clear();
            RegisterIoC();

            var Assemblies = new List<Assembly>(4);
            switch (Project)
            {
                case UnitTestProject.Client:
                    Assemblies.Add(AppDomain.CurrentDomain.GetAssemblyByName("SS14.Client"));
                    break;
                case UnitTestProject.Server:
                    Assemblies.Add(AppDomain.CurrentDomain.GetAssemblyByName("SS14.Server"));
                    break;
                default:
                    throw new NotSupportedException($"Unknown testing project: {Project}");
            }

            Assemblies.Add(AppDomain.CurrentDomain.GetAssemblyByName("SS14.Shared"));
            Assemblies.Add(Assembly.GetExecutingAssembly());

            IoCManager.Resolve<IReflectionManager>().LoadAssemblies(Assemblies);

            if (NeedsClientConfig)
            {
                //ConfigurationManager setup
                GetConfigurationManager = IoCManager.Resolve<IConfigurationManager>();
                GetConfigurationManager.LoadFromFile(PathHelpers.ExecutableRelativeFile("./client_config.toml"));
            }

            /*
            if (NeedsResourcePack)
            {
                GetResourceCache = IoCManager.Resolve<IResourceCache>();
                InitializeResources();
            }
            */
        }

        #region Setup

        /// <summary>
        /// Registers all the types into the <see cref="IoCManager"/> with <see cref="IoCManager.Register{TInterface, TImplementation}"/>
        /// </summary>
        private void RegisterIoC()
        {
            // Shared stuff.
            IoCManager.Register<IComponentManager, ComponentManager>();
            IoCManager.Register<IEntitySystemManager, EntitySystemManager>();
            IoCManager.Register<IConfigurationManager, ConfigurationManager>();
            IoCManager.Register<ISS14Serializer, SS14Serializer>();
            IoCManager.Register<INetManager, NetManager>();
            IoCManager.Register<IGameTiming, GameTiming>();
            IoCManager.Register<ITimerManager, TimerManager>();
            IoCManager.Register<ILogManager, LogManager>();
            IoCManager.Register<ITaskManager, TaskManager>();

            switch (Project)
            {
                case UnitTestProject.Client:
                    IoCManager.Register<ITileDefinitionManager, TileDefinitionManager>();
                    IoCManager.Register<IEntityManager, ClientEntityManager>();
                    IoCManager.Register<IComponentFactory, ComponentFactory>();
                    IoCManager.Register<IMapManager, MapManager>();
                    IoCManager.Register<ICollisionManager, CollisionManager>();

                    // Client stuff.
                    IoCManager.Register<IReflectionManager, ClientReflectionManager>();
                    IoCManager.Register<IResourceManager, ResourceCache>();
                    IoCManager.Register<IResourceManagerInternal, ResourceCache>();
                    IoCManager.Register<IResourceCache, ResourceCache>();
                    IoCManager.Register<IClientNetManager, NetManager>();
                    IoCManager.Register<IClientEntityManager, ClientEntityManager>();
                    IoCManager.Register<IEntityNetworkManager, ClientEntityNetworkManager>();
                    IoCManager.Register<IClientGameStateManager, ClientGameStateManager>();
                    IoCManager.Register<IBaseClient, BaseClient>();
                    IoCManager.Register<SS14.Client.Interfaces.Player.IPlayerManager, SS14.Client.Player.PlayerManager>();
                    IoCManager.Register<IStateManager, StateManager>();
                    IoCManager.Register<IUserInterfaceManager, DummyUserInterfaceManager>();
                    IoCManager.Register<IGameControllerProxy, GameControllerProxyDummy>();
                    IoCManager.Register<IInputManager, InputManager>();
                    IoCManager.Register<IDebugDrawing, DebugDrawing>();
                    IoCManager.Register<IClientConsole, ClientChatConsole>();
                    IoCManager.Register<IClientChatConsole, ClientChatConsole>();
                    //IoCManager.Register<ILightManager, LightManager>();
                    IoCManager.Register<IDisplayManager, DisplayManager>();
                    //IoCManager.Register<IEyeManager, EyeManager>();
                    IoCManager.Register<IPrototypeManager, PrototypeManager>();
                    IoCManager.Register<IOverlayManager, OverlayManager>();
                    IoCManager.Register<ISceneTreeHolder, SceneTreeHolder>();
                    IoCManager.Register<IViewVariablesManager, ViewVariablesManager>();
                    break;

                case UnitTestProject.Server:
                    IoCManager.Register<IResourceManager, ResourceManager>();
                    IoCManager.Register<IResourceManagerInternal, ResourceManager>();
                    IoCManager.Register<IEntityManager, ServerEntityManager>();
                    IoCManager.Register<IServerEntityManager, ServerEntityManager>();
                    IoCManager.Register<IChatManager, ChatManager>();
                    IoCManager.Register<IServerNetManager, NetManager>();
                    IoCManager.Register<IMapManager, MapManager>();
                    IoCManager.Register<IPlacementManager, PlacementManager>();
                    IoCManager.Register<ISystemConsoleManager, SystemConsoleManager>();
                    IoCManager.Register<ITileDefinitionManager, TileDefinitionManager>();
                    IoCManager.Register<IEntityNetworkManager, ServerEntityNetworkManager>();
                    IoCManager.Register<ICommandLineArgs, CommandLineArgs>();
                    IoCManager.Register<IServerGameStateManager, ServerGameStateManager>();
                    IoCManager.Register<IReflectionManager, ServerReflectionManager>();
                    IoCManager.Register<IConsoleShell, ConsoleShell>();
                    IoCManager.Register<IPlayerManager, PlayerManager>();
                    IoCManager.Register<IComponentFactory, ServerComponentFactory>();
                    IoCManager.Register<IBaseServer, BaseServer>();
                    IoCManager.Register<IMapLoader, MapLoader>();
                    IoCManager.Register<IPrototypeManager, ServerPrototypeManager>();
                    IoCManager.Register<IViewVariablesHost, ViewVariablesHost>();
                    IoCManager.Register<IConGroupController, ConGroupController>();
                    break;

                default:
                    throw new NotSupportedException($"Unknown testing project: {Project}");
            }

            OverrideIoC();

            IoCManager.BuildGraph();
        }

        /// <summary>
        /// Called after all IoC registration has been done, but before the graph has been built.
        /// This allows one to add new IoC types or overwrite existing ones if needed.
        /// </summary>
        protected virtual void OverrideIoC() { }

        #endregion Setup
    }
}
