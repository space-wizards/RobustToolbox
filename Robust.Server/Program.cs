using Robust.Server.GameObjects;
using Robust.Server.GameStates;
using Robust.Server.Interfaces;
using Robust.Server.Interfaces.GameObjects;
using Robust.Server.Interfaces.GameState;
using Robust.Server.Interfaces.Maps;
using Robust.Server.Interfaces.Placement;
using Robust.Server.Interfaces.Player;
using Robust.Server.Maps;
using Robust.Server.Placement;
using Robust.Server.Player;
using Robust.Server.Prototypes;
using Robust.Server.Reflection;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Log;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.Interfaces.Reflection;
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
using System;
using System.Collections.Generic;
using System.Reflection;
using Robust.Shared.Interfaces.Resources;
using Robust.Server.Console;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.ServerStatus;
using Robust.Server.Interfaces.Timing;
using Robust.Server.ServerStatus;
using Robust.Server.Timing;
using Robust.Server.ViewVariables;
using Robust.Shared.Asynchronous;
using Robust.Shared.Exceptions;
using Robust.Shared.Localization;

namespace Robust.Server
{
    internal static class EntryPoint
    {
        internal static void Main(string[] args)
        {
#if !X64
            throw new InvalidOperationException("The server cannot start outside x64.");
#endif
            IoCManager.InitThread();
            RegisterIoC();
            IoCManager.BuildGraph();
            SetupLogging();
            InitReflectionManager();
            HandleCommandLineArgs(args);

            var server = IoCManager.Resolve<IBaseServer>();

            Logger.Info("Server -> Starting");

            if (server.Start())
            {
                Logger.Fatal("Server -> Can not start server");
                //Not like you'd see this, haha. Perhaps later for logging.
                //Environment.Exit(0);
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

        private static void HandleCommandLineArgs(string[] args)
        {
            var commandLine = IoCManager.Resolve<ICommandLineArgs>();
            if (!commandLine.Parse(args))
            {
                Environment.Exit(0);
            }
        }

        /// <summary>
        /// Registers all the types into the <see cref="IoCManager"/> with <see cref="IoCManager.Register{TInterface, TImplementation}"/>
        /// </summary>
        internal static void RegisterIoC()
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
            IoCManager.Register<IDynamicTypeFactory, DynamicTypeFactory>();
            IoCManager.Register<ILocalizationManager, LocalizationManager>();

            // Server stuff.
            IoCManager.Register<IEntityManager, ServerEntityManager>();
            IoCManager.Register<IServerEntityManager, ServerEntityManager>();
            IoCManager.Register<IServerEntityManagerInternal, ServerEntityManager>();
            IoCManager.Register<IServerNetManager, NetManager>();
            IoCManager.Register<IMapManager, MapManager>();
            IoCManager.Register<IPlacementManager, PlacementManager>();
            IoCManager.Register<ISystemConsoleManager, SystemConsoleManager>();
            IoCManager.Register<ITileDefinitionManager, TileDefinitionManager>();
            IoCManager.Register<IBaseServer, BaseServer>();
            IoCManager.Register<IBaseServerInternal, BaseServer>();
            IoCManager.Register<IRobustSerializer, RobustSerializer>();
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
            IoCManager.Register<IModLoader, ModLoader>();
        }

        internal static void InitReflectionManager()
        {
            // gets a handle to the shared and the current (server) dll.
            IoCManager.Resolve<IReflectionManager>().LoadAssemblies(new List<Assembly>(2)
            {
                AppDomain.CurrentDomain.GetAssemblyByName("Robust.Shared"),
                Assembly.GetExecutingAssembly()
            });
        }

        internal static void SetupLogging()
        {
            var mgr = IoCManager.Resolve<ILogManager>();
            var handler = new ConsoleLogHandler();
            mgr.RootSawmill.AddHandler(handler);
            mgr.GetSawmill("res.typecheck").Level = LogLevel.Info;
            mgr.GetSawmill("go.sys").Level = LogLevel.Info;
        }
    }
}
