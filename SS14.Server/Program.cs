using SS14.Server.Chat;
using SS14.Server.GameObjects;
using SS14.Server.GameStates;
using SS14.Server.Interfaces;
using SS14.Server.Interfaces.Chat;
using SS14.Server.Interfaces.ClientConsoleHost;
using SS14.Server.Interfaces.GameObjects;
using SS14.Server.Interfaces.GameState;
using SS14.Server.Interfaces.Log;
using SS14.Server.Interfaces.Maps;
using SS14.Server.Interfaces.Placement;
using SS14.Server.Interfaces.Player;
using SS14.Server.Interfaces.ServerConsole;
using SS14.Server.Log;
using SS14.Server.Maps;
using SS14.Server.Placement;
using SS14.Server.Player;
using SS14.Server.Reflection;
using SS14.Server.ServerConsole;
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
using System;
using System.Collections.Generic;
using System.Reflection;

namespace SS14.Server
{
    internal class EntryPoint
    {
        private static void Main(string[] args)
        {
            //Register minidump dumper only if the app isn't being debugged. No use filling up hard drives with shite
            RegisterIoC();
            LoadContentAssemblies();
            HandleCommandLineArgs();

            var server = IoCManager.Resolve<IBaseServer>();

            Logger.Log("Server -> Starting");

            if (server.Start())
            {
                Logger.Log("Server -> Can not start server", LogLevel.Fatal);
                //Not like you'd see this, haha. Perhaps later for logging.
                Environment.Exit(0);
            }

            string strVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            Logger.Log("Server Version " + strVersion + " -> Ready");

            // TODO: Move this to an interface.
            SignalHander.InstallSignals();

            server.MainLoop();

            Logger.Info("Goodbye.");

            // Used to dispose of systems that want to be disposed.
            // Such as the log manager.
            IoCManager.Clear();
        }

        private static void HandleCommandLineArgs()
        {
            var commandLine = IoCManager.Resolve<ICommandLineArgs>();
            if (!commandLine.Parse())
            {
                Environment.Exit(0);
            }
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
            IoCManager.Register<IConfigurationManager, ConfigurationManager>();
            IoCManager.Register<INetManager, NetManager>();
            IoCManager.Register<IGameTiming, GameTiming>();
            IoCManager.Register<IResourceManager, ResourceManager>();
            IoCManager.Register<ICollisionManager, CollisionManager>();
            IoCManager.Register<ITimerManager, TimerManager>();

            // Server stuff.
            IoCManager.Register<IEntityManager, ServerEntityManager>();
            IoCManager.Register<IServerEntityManager, ServerEntityManager>();
            IoCManager.Register<ILogManager, ServerLogManager>();
            IoCManager.Register<IServerLogManager, ServerLogManager>();
            IoCManager.Register<IChatManager, ChatManager>();
            IoCManager.Register<IServerNetManager, NetManager>();
            IoCManager.Register<IMapManager, MapManager>();
            IoCManager.Register<IPlacementManager, PlacementManager>();
            IoCManager.Register<IConsoleManager, ConsoleManager>();
            IoCManager.Register<ITileDefinitionManager, TileDefinitionManager>();
            IoCManager.Register<IBaseServer, BaseServer>();
            IoCManager.Register<ISS14Serializer, SS14Serializer>();
            IoCManager.Register<IEntityNetworkManager, ServerEntityNetworkManager>();
            IoCManager.Register<ICommandLineArgs, CommandLineArgs>();
            IoCManager.Register<IGameStateManager, GameStateManager>();
            IoCManager.Register<IReflectionManager, ServerReflectionManager>();
            IoCManager.Register<IClientConsoleHost, ClientConsoleHost.ClientConsoleHost>();
            IoCManager.Register<IPlayerManager, PlayerManager>();
            IoCManager.Register<IComponentFactory, ServerComponentFactory>();
            IoCManager.Register<IMapLoader, MapLoader>();

            IoCManager.BuildGraph();
        }

        private static void LoadContentAssemblies()
        {
            // gets a handle to the shared and the current (server) dll.
            IoCManager.Resolve<IReflectionManager>().LoadAssemblies(new List<Assembly>(2)
            {
                AppDomain.CurrentDomain.GetAssemblyByName("SS14.Shared"),
                Assembly.GetExecutingAssembly()
            });
        }
    }
}
