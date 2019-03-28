using SS14.Server.Chat;
using SS14.Server.GameObjects;
using SS14.Server.GameStates;
using SS14.Server.Interfaces;
using SS14.Server.Interfaces.Chat;
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
using SS14.Shared.Interfaces.Resources;
using SS14.Server.Console;
using SS14.Server.Interfaces.Console;
using SS14.Server.Interfaces.ServerStatus;
using SS14.Server.Interfaces.Timing;
using SS14.Server.ServerStatus;
using SS14.Server.Timing;
using SS14.Server.ViewVariables;
using SS14.Shared.Asynchronous;
using SS14.Shared.Exceptions;

namespace SS14.Server
{
    internal class EntryPoint
    {
        private static void Main(string[] args)
        {
#if !X64
            throw new InvalidOperationException("The server cannot start outside x64.");
#endif
            //Register minidump dumper only if the app isn't being debugged. No use filling up hard drives with shite
            RegisterIoC();
            SetupLogging();
            InitReflectionManager();
            HandleCommandLineArgs();

            var server = IoCManager.Resolve<IBaseServer>();

            Logger.Info("Server -> Starting");

            if (server.Start())
            {
                Logger.Fatal("Server -> Can not start server");
                //Not like you'd see this, haha. Perhaps later for logging.
                Environment.Exit(0);
            }

            string strVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            Logger.Info("Server Version " + strVersion + " -> Ready");

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
            IoCManager.Register<IEntitySystemManager, EntitySystemManager>();
            IoCManager.Register<IConfigurationManager, ConfigurationManager>();
            IoCManager.Register<INetManager, NetManager>();
            IoCManager.Register<IGameTiming, GameTiming>();
            IoCManager.Register<IResourceManager, ResourceManager>();
            IoCManager.Register<IResourceManagerInternal, ResourceManager>();
            IoCManager.Register<IPhysicsManager, PhysicsManager>();
            IoCManager.Register<ITimerManager, TimerManager>();
            IoCManager.Register<ILogManager, LogManager>();
            IoCManager.Register<ITaskManager, TaskManager>();
            IoCManager.Register<IRuntimeLog, RuntimeLog>();

            // Server stuff.
            IoCManager.Register<IEntityManager, ServerEntityManager>();
            IoCManager.Register<IServerEntityManager, ServerEntityManager>();
            IoCManager.Register<IServerEntityManagerInternal, ServerEntityManager>();
            IoCManager.Register<IChatManager, ChatManager>();
            IoCManager.Register<IServerNetManager, NetManager>();
            IoCManager.Register<IMapManager, MapManager>();
            IoCManager.Register<IPlacementManager, PlacementManager>();
            IoCManager.Register<ISystemConsoleManager, SystemConsoleManager>();
            IoCManager.Register<ITileDefinitionManager, TileDefinitionManager>();
            IoCManager.Register<IBaseServer, BaseServer>();
            IoCManager.Register<ISS14Serializer, SS14Serializer>();
            IoCManager.Register<IEntityNetworkManager, ServerEntityNetworkManager>();
            IoCManager.Register<ICommandLineArgs, CommandLineArgs>();
            IoCManager.Register<IServerGameStateManager, ServerGameStateManager>();
            IoCManager.Register<IReflectionManager, ServerReflectionManager>();
            IoCManager.Register<IConsoleShell, ConsoleShell>();
            IoCManager.Register<IPlayerManager, PlayerManager>();
            IoCManager.Register<IComponentFactory, ServerComponentFactory>();
            IoCManager.Register<IMapLoader, MapLoader>();
            IoCManager.Register<IPrototypeManager, ServerPrototypeManager>();
            IoCManager.Register<IViewVariablesHost, ViewVariablesHost>();
            IoCManager.Register<IConGroupController, ConGroupController>();
            IoCManager.Register<IStatusHost, StatusHost>();
            IoCManager.Register<IPauseManager, PauseManager>();

            IoCManager.BuildGraph();
        }

        private static void InitReflectionManager()
        {
            // gets a handle to the shared and the current (server) dll.
            IoCManager.Resolve<IReflectionManager>().LoadAssemblies(new List<Assembly>(2)
            {
                AppDomain.CurrentDomain.GetAssemblyByName("SS14.Shared"),
                Assembly.GetExecutingAssembly()
            });
        }

        private static void SetupLogging()
        {
            var mgr = IoCManager.Resolve<ILogManager>();
            var handler = new ConsoleLogHandler();
            mgr.RootSawmill.AddHandler(handler);
            mgr.GetSawmill("res.typecheck").Level = LogLevel.Info;
            mgr.GetSawmill("go.sys").Level = LogLevel.Info;
        }
    }
}
