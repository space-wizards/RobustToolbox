using SS14.Client.GameObjects;
using SS14.Client.Input;
using SS14.Client.Interfaces;
using SS14.Client.Interfaces.GameObjects;
using SS14.Client.Interfaces.Input;
using SS14.Client.Interfaces.Network;
using SS14.Client.Interfaces.Placement;
using SS14.Client.Interfaces.Player;
using SS14.Client.Interfaces.Resource;
using SS14.Client.Interfaces.State;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.Interfaces.Utility;
using SS14.Client.Network;
using SS14.Client.Placement;
using SS14.Client.Player;
using SS14.Client.Reflection;
using SS14.Client.Resources;
using SS14.Client.State;
using SS14.Client.UserInterface;
using SS14.Client.Utility;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Log;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.Interfaces.Reflection;
using SS14.Shared.Interfaces.Serialization;
using SS14.Shared.Interfaces.Timing;
using SS14.Shared.Configuration;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Prototypes;
using SS14.Shared.Serialization;
using SS14.Shared.Timing;
using System;
using System.Collections.Generic;
using System.Reflection;
using SS14.Shared.ContentPack;
using SS14.Shared.Interfaces;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Interfaces.Physics;
using SS14.Shared.Map;
using SS14.Shared.Network;
using SS14.Shared.Physics;
using SS14.Client.Interfaces.GameStates;
using SS14.Client.GameStates;
using SS14.Client.Graphics.Lighting;

namespace SS14.Client
{
    public class Program
    {
        /************************************************************************/
        /* program starts here                                                  */
        /************************************************************************/

        [STAThread]
        private static void Main()
        {
            RegisterIoC();
            RegisterComponents();

            var controller = IoCManager.Resolve<IGameController>();
            controller.Run();

            Logger.Info("Goodbye.");
            IoCManager.Clear();
        }

        /// <summary>
        /// Registers all the types into the <see cref="IoCManager"/> with <see cref="IoCManager.Register{TInterface, TImplementation}"/>
        /// </summary>
        private static void RegisterIoC()
        {
            // Shared stuff.
            IoCManager.Register<IComponentManager, ComponentManager>();
            IoCManager.Register<IPrototypeManager, PrototypeManager>();
            IoCManager.Register<IEntitySystemManager, EntitySystemManager>();
            IoCManager.Register<ILogManager, LogManager>();
            IoCManager.Register<IConfigurationManager, ConfigurationManager>();
            IoCManager.Register<INetManager, NetManager>();
            IoCManager.Register<IGameTiming, GameTiming>();
            IoCManager.Register<IResourceManager, ResourceManager>();
            IoCManager.Register<ICollisionManager, CollisionManager>();

            // Client stuff.
            IoCManager.Register<IRand, Rand>();
            IoCManager.Register<IStateManager, StateManager>();
            IoCManager.Register<INetworkGrapher, NetworkGrapher>();
            IoCManager.Register<IKeyBindingManager, KeyBindingManager>();
            IoCManager.Register<IUserInterfaceManager, UserInterfaceManager>();
            IoCManager.Register<ITileDefinitionManager, TileDefinitionManager>();
            IoCManager.Register<IEntityManager, ClientEntityManager>();
            IoCManager.Register<IClientEntityManager, ClientEntityManager>();
            IoCManager.Register<IClientNetManager, NetManager>();
            IoCManager.Register<IReflectionManager, ClientReflectionManager>();
            IoCManager.Register<IPlacementManager, PlacementManager>();
            IoCManager.Register<ILightManager, LightManager>();
            IoCManager.Register<IResourceCache, ResourceCache>();
            IoCManager.Register<ISS14Serializer, SS14Serializer>();
            IoCManager.Register<IMapManager, MapManager>();
            IoCManager.Register<IEntityNetworkManager, ClientEntityNetworkManager>();
            IoCManager.Register<IPlayerManager, PlayerManager>();
            IoCManager.Register<IGameController, GameController>();
            IoCManager.Register<IComponentFactory, ClientComponentFactory>();
            IoCManager.Register<IGameStateManager, GameStateManager>();

            IoCManager.BuildGraph();
        }

        private static void RegisterComponents()
        {
            // gets a handle to the shared and the current (client) dll.
            IoCManager.Resolve<IReflectionManager>().LoadAssemblies(new List<Assembly>(2)
            {
                AppDomain.CurrentDomain.GetAssemblyByName("SS14.Shared"),
                AppDomain.CurrentDomain.GetAssemblyByName("SS14.Client.Graphics"),
                Assembly.GetExecutingAssembly()
            });
        }
    }
}
