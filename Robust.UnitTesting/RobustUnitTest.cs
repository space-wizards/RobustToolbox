using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Robust.Client;
using Robust.Client.Console;
using Robust.Client.Debugging;
using Robust.Client.GameObjects;
using Robust.Client.GameStates;
using Robust.Client.Graphics;
using Robust.Client.Graphics.ClientEye;
using Robust.Client.Graphics.Clyde;
using Robust.Client.Graphics.Overlays;
using Robust.Client.Input;
using Robust.Client.Interfaces;
using Robust.Client.Interfaces.Debugging;
using Robust.Client.Interfaces.GameObjects;
using Robust.Client.Interfaces.GameStates;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Client.Interfaces.Graphics.Overlays;
using Robust.Client.Interfaces.Input;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.Interfaces.State;
using Robust.Client.Interfaces.UserInterface;
using Robust.Client.Interfaces.Utility;
using Robust.Client.Reflection;
using Robust.Client.ResourceManagement;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Client.Utility;
using Robust.Client.ViewVariables;
using Robust.Server;
using Robust.Server.Console;
using Robust.Server.GameObjects;
using Robust.Server.GameStates;
using Robust.Server.Interfaces;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.GameObjects;
using Robust.Server.Interfaces.GameState;
using Robust.Server.Interfaces.Maps;
using Robust.Server.Interfaces.Placement;
using Robust.Server.Interfaces.ServerStatus;
using Robust.Server.Interfaces.Timing;
using Robust.Server.Maps;
using Robust.Server.Placement;
using Robust.Server.Player;
using Robust.Server.Prototypes;
using Robust.Server.Reflection;
using Robust.Server.ServerStatus;
using Robust.Server.Timing;
using Robust.Server.ViewVariables;
using Robust.Shared.Asynchronous;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Exceptions;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Log;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.Interfaces.Resources;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Interfaces.Timers;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timers;
using Robust.Shared.Timing;
using Robust.UnitTesting.Client;
using IPlayerManager = Robust.Client.Player.IPlayerManager;

namespace Robust.UnitTesting
{
    public enum UnitTestProject
    {
        Server,
        Client
    }

    [Parallelizable]
    public abstract class RobustUnitTest
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
            IoCManager.InitThread();
            IoCManager.Clear();
            RegisterIoC();

            var Assemblies = new List<Assembly>(4);
            switch (Project)
            {
                case UnitTestProject.Client:
                    Assemblies.Add(AppDomain.CurrentDomain.GetAssemblyByName("Robust.Client"));
                    break;
                case UnitTestProject.Server:
                    Assemblies.Add(AppDomain.CurrentDomain.GetAssemblyByName("Robust.Server"));
                    break;
                default:
                    throw new NotSupportedException($"Unknown testing project: {Project}");
            }

            Assemblies.Add(AppDomain.CurrentDomain.GetAssemblyByName("Robust.Shared"));
            Assemblies.Add(Assembly.GetExecutingAssembly());

            IoCManager.Resolve<IReflectionManager>().LoadAssemblies(Assemblies);

            if (NeedsClientConfig)
            {
                //ConfigurationManager setup
                GetConfigurationManager = IoCManager.Resolve<IConfigurationManager>();
                GetConfigurationManager.LoadFromFile(PathHelpers.ExecutableRelativeFile("./client_config.toml"));
            }

            // Required components for the engine to work
            var compFactory = IoCManager.Resolve<IComponentFactory>();
            if (!compFactory.AllRegisteredTypes.Contains(typeof(MetaDataComponent)))
            {
                compFactory.Register<MetaDataComponent>();
                compFactory.RegisterReference<MetaDataComponent, IMetaDataComponent>();
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
            IoCManager.Register<IRobustSerializer, RobustSerializer>();
            IoCManager.Register<INetManager, NetManager>();
            IoCManager.Register<IGameTiming, GameTiming>();
            IoCManager.Register<ITimerManager, TimerManager>();
            IoCManager.Register<ILogManager, LogManager>();
            IoCManager.Register<ITaskManager, TaskManager>();
            IoCManager.Register<IRuntimeLog, RuntimeLog>();
            IoCManager.Register<IDynamicTypeFactory, DynamicTypeFactory>();

            switch (Project)
            {
                case UnitTestProject.Client:
                    IoCManager.Register<ITileDefinitionManager, TileDefinitionManager>();
                    IoCManager.Register<IEntityManager, ClientEntityManager>();
                    IoCManager.Register<IComponentFactory, ComponentFactory>();
                    IoCManager.Register<IMapManager, MapManager>();
                    IoCManager.Register<IPhysicsManager, PhysicsManager>();

                    // Client stuff.
                    IoCManager.Register<IGameControllerInternal, GameControllerDummy>();
                    IoCManager.Register<IGameController, GameControllerDummy>();
                    IoCManager.Register<IReflectionManager, ClientReflectionManager>();
                    IoCManager.Register<IResourceManager, ResourceCache>();
                    IoCManager.Register<IResourceManagerInternal, ResourceCache>();
                    IoCManager.Register<IResourceCache, ResourceCache>();
                    IoCManager.Register<IClientNetManager, NetManager>();
                    IoCManager.Register<IClientEntityManager, ClientEntityManager>();
                    IoCManager.Register<IEntityNetworkManager, ClientEntityNetworkManager>();
                    IoCManager.Register<IClientGameStateManager, ClientGameStateManager>();
                    IoCManager.Register<IBaseClient, BaseClient>();
                    IoCManager.Register<IPlayerManager, Robust.Client.Player.PlayerManager>();
                    IoCManager.Register<IStateManager, StateManager>();
                    IoCManager.Register<IUserInterfaceManager, UserInterfaceManager>();
                    IoCManager.Register<IUserInterfaceManagerInternal, UserInterfaceManager>();
                    IoCManager.Register<IInputManager, InputManager>();
                    IoCManager.Register<IDebugDrawing, DebugDrawing>();
                    //IoCManager.Register<ILightManager, LightManager>();
                    IoCManager.Register<IDisplayManager, ClydeHeadless>();
                    IoCManager.Register<IClyde, ClydeHeadless>();
                    //IoCManager.Register<IEyeManager, EyeManager>();
                    IoCManager.Register<IPrototypeManager, PrototypeManager>();
                    IoCManager.Register<IOverlayManager, OverlayManager>();
                    IoCManager.Register<IViewVariablesManager, ViewVariablesManager>();
                    IoCManager.Register<IClipboardManager, ClipboardManagerUnsupported>();
                    IoCManager.Register<IDiscordRichPresence, DiscordRichPresence>();
                    IoCManager.Register<IEyeManager, EyeManager>();
                    IoCManager.Register<IClientConsole, ClientConsole>();
                    break;

                case UnitTestProject.Server:
                    IoCManager.Register<IResourceManager, ResourceManager>();
                    IoCManager.Register<IResourceManagerInternal, ResourceManager>();
                    IoCManager.Register<IEntityManager, ServerEntityManager>();
                    IoCManager.Register<IServerEntityManager, ServerEntityManager>();
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
                    IoCManager.Register<Robust.Server.Interfaces.Player.IPlayerManager, PlayerManager>();
                    IoCManager.Register<IComponentFactory, ServerComponentFactory>();
                    IoCManager.Register<IBaseServer, BaseServer>();
                    IoCManager.Register<IMapLoader, MapLoader>();
                    IoCManager.Register<IPrototypeManager, ServerPrototypeManager>();
                    IoCManager.Register<IViewVariablesHost, ViewVariablesHost>();
                    IoCManager.Register<IConGroupController, ConGroupController>();
                    IoCManager.Register<IStatusHost, StatusHost>();
                    IoCManager.Register<IPauseManager, PauseManager>();
                    IoCManager.Register<IServerEntityManagerInternal, ServerEntityManager>();
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
