using Robust.Server.Console;
using Robust.Server.DataMetrics;
using Robust.Server.Debugging;
using Robust.Server.GameObjects;
using Robust.Server.GameStates;
using Robust.Server.Map;
using Robust.Server.Maps;
using Robust.Server.Placement;
using Robust.Server.Player;
using Robust.Server.Prototypes;
using Robust.Server.Reflection;
using Robust.Server.Scripting;
using Robust.Server.ServerStatus;
using Robust.Server.ViewVariables;
using Robust.Shared;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;
using Robust.Shared.Timing;

namespace Robust.Server
{
    internal static class ServerIoC
    {
        /// <summary>
        /// Registers all the types into the <see cref="IoCManager"/> with <see cref="IoCManager.Register{TInterface, TImplementation}"/>
        /// </summary>
        internal static void RegisterIoC()
        {
            SharedIoC.RegisterIoC();

            IoCManager.Register<IBaseServer, BaseServer>();
            IoCManager.Register<IBaseServerInternal, BaseServer>();
            IoCManager.Register<IComponentFactory, ServerComponentFactory>();
            IoCManager.Register<IConGroupController, ConGroupController>();
            IoCManager.Register<IServerConsoleHost, ServerConsoleHost>();
            IoCManager.Register<IConsoleHost, ServerConsoleHost>();
            IoCManager.Register<IMapManager, ServerMapManager>();
            IoCManager.Register<IMapManagerInternal, ServerMapManager>();
            IoCManager.Register<IServerMapManager, ServerMapManager>();
            IoCManager.Register<IEntityManager, ServerEntityManager>();
            IoCManager.Register<IEntityLookup, SharedEntityLookup>();
            IoCManager.Register<IEntityNetworkManager, ServerEntityNetworkManager>();
            IoCManager.Register<IServerEntityNetworkManager, ServerEntityNetworkManager>();
            IoCManager.Register<IMapLoader, MapLoader>();
            IoCManager.Register<IPlacementManager, PlacementManager>();
            IoCManager.Register<IPlayerManager, PlayerManager>();
            IoCManager.Register<ISharedPlayerManager, PlayerManager>();
            IoCManager.Register<IPrototypeManager, ServerPrototypeManager>();
            IoCManager.Register<IReflectionManager, ServerReflectionManager>();
            IoCManager.Register<IResourceManager, ResourceManager>();
            IoCManager.Register<IResourceManagerInternal, ResourceManager>();
            IoCManager.Register<IServerEntityManager, ServerEntityManager>();
            IoCManager.Register<IServerEntityManagerInternal, ServerEntityManager>();
            IoCManager.Register<IServerGameStateManager, ServerGameStateManager>();
            IoCManager.Register<IServerNetManager, NetManager>();
            IoCManager.Register<IStatusHost, StatusHost>();
            IoCManager.Register<ISystemConsoleManager, SystemConsoleManager>();
            IoCManager.Register<ITileDefinitionManager, TileDefinitionManager>();
            IoCManager.Register<IViewVariablesHost, ViewVariablesHost>();
            IoCManager.Register<IDebugDrawingManager, DebugDrawingManager>();
            IoCManager.Register<IWatchdogApi, WatchdogApi>();
            IoCManager.Register<IScriptHost, ScriptHost>();
            IoCManager.Register<IMetricsManager, MetricsManager>();
            IoCManager.Register<IAuthManager, AuthManager>();
        }
    }
}
