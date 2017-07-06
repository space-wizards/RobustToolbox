using SS14.Client.Collision;
using SS14.Client.GameObjects;
using SS14.Client.Input;
using SS14.Client.Interfaces;
using SS14.Client.Interfaces.Collision;
using SS14.Client.Interfaces.GameObjects;
using SS14.Client.Interfaces.GameTimer;
using SS14.Client.Interfaces.Input;
using SS14.Client.Interfaces.Lighting;
using SS14.Client.Interfaces.Map;
using SS14.Client.Interfaces.MessageLogging;
using SS14.Client.Interfaces.Network;
using SS14.Client.Interfaces.Placement;
using SS14.Client.Interfaces.Player;
using SS14.Client.Interfaces.Resource;
using SS14.Client.Interfaces.Serialization;
using SS14.Client.Interfaces.State;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.Interfaces.Utility;
using SS14.Client.Lighting;
using SS14.Client.Map;
using SS14.Client.MessageLogging;
using SS14.Client.Network;
using SS14.Client.Placement;
using SS14.Client.Player;
using SS14.Client.Reflection;
using SS14.Client.Resources;
using SS14.Client.Serialization;
using SS14.Client.State;
using SS14.Client.UserInterface;
using SS14.Client.Utility;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Log;
using SS14.Shared.Interfaces.Reflection;
using SS14.Shared.Configuration;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Prototypes;
using SS14.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Reflection;

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
            LoadAssemblies();

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
            IoCManager.Register<IComponentFactory, ComponentFactory>();
            IoCManager.Register<ILogManager, LogManager>();
            IoCManager.Register<IConfigurationManager, ConfigurationManager>();

            // Client stuff.
            IoCManager.Register<IRand, Rand>();
            IoCManager.Register<IStateManager, StateManager>();
            IoCManager.Register<INetworkGrapher, NetworkGrapher>();
            IoCManager.Register<IKeyBindingManager, KeyBindingManager>();
            IoCManager.Register<IUserInterfaceManager, UserInterfaceManager>();
            IoCManager.Register<IGameTimer, GameTimer.GameTimer>();
            IoCManager.Register<ITileDefinitionManager, TileDefinitionManager>();
            IoCManager.Register<IMessageLogger, MessageLogger>();
            IoCManager.Register<ICollisionManager, CollisionManager>();
            IoCManager.Register<IEntityManager, ClientEntityManager>();
            IoCManager.Register<IClientEntityManager, ClientEntityManager>();
            IoCManager.Register<INetworkManager, NetworkManager>();
            IoCManager.Register<IReflectionManager, ClientReflectionManager>();
            IoCManager.Register<IPlacementManager, PlacementManager>();
            IoCManager.Register<ILightManager, LightManager>();
            IoCManager.Register<IResourceManager, ResourceManager>();
            IoCManager.Register<ISS14Serializer, SS14Serializer>();
            IoCManager.Register<IMapManager, MapManager>();
            IoCManager.Register<IEntityNetworkManager, ClientEntityNetworkManager>();
            IoCManager.Register<IPlayerManager, PlayerManager>();
            IoCManager.Register<IGameController, GameController>();

            IoCManager.BuildGraph();
        }

        private static void LoadAssemblies()
        {
            var assemblies = new List<Assembly>(4)
            {
                AppDomain.CurrentDomain.GetAssemblyByName("SS14.Shared"),
                Assembly.GetExecutingAssembly()
            };

            // TODO this should be done on connect.
            // The issue is that due to our giant trucks of shit code.
            // It'd be extremely hard to integrate correctly.
            try
            {
                var contentAssembly = AssemblyHelpers.RelativeLoadFrom("SS14.Shared.Content.dll");
                assemblies.Add(contentAssembly);
            }
            catch (Exception e)
            {
                // LogManager won't work yet.
                System.Console.ForegroundColor = ConsoleColor.Red;
                System.Console.WriteLine("**ERROR: Unable to load the shared content assembly (SS14.Shared.Content.dll): {0}", e);
                System.Console.ResetColor();
            }

            try
            {
                var contentAssembly = AssemblyHelpers.RelativeLoadFrom("SS14.Server.Content.dll");
                assemblies.Add(contentAssembly);
            }
            catch (Exception e)
            {
                // LogManager won't work yet.
                System.Console.ForegroundColor = ConsoleColor.Red;
                System.Console.WriteLine("**ERROR: Unable to load the server content assembly (SS14.Server.Content.dll): {0}", e);
                System.Console.ResetColor();
            }

            IoCManager.Resolve<IReflectionManager>().LoadAssemblies(assemblies);
        }
    }
}
