using Robust.Shared.Asynchronous;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Exceptions;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Random;
using Robust.Shared.Sandboxing;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Timing;

namespace Robust.Shared
{
    internal static class SharedIoC
    {
        public static void RegisterIoC()
        {
            IoCManager.Register<ISerializationManager, SerializationManager>();
            IoCManager.Register<IConfigurationManager, NetConfigurationManager>();
            IoCManager.Register<INetConfigurationManager, NetConfigurationManager>();
            IoCManager.Register<IConfigurationManagerInternal, NetConfigurationManager>();
            IoCManager.Register<IDynamicTypeFactory, DynamicTypeFactory>();
            IoCManager.Register<IDynamicTypeFactoryInternal, DynamicTypeFactory>();
            IoCManager.Register<IEntitySystemManager, EntitySystemManager>();
            IoCManager.Register<ILocalizationManager, LocalizationManager>();
            IoCManager.Register<ILocalizationManagerInternal, LocalizationManager>();
            IoCManager.Register<ILogManager, LogManager>();
            IoCManager.Register<IPauseManager, NetworkedMapManager>();
            IoCManager.Register<IModLoader, ModLoader>();
            IoCManager.Register<IModLoaderInternal, ModLoader>();
            IoCManager.Register<INetManager, NetManager>();
            IoCManager.Register<IRobustSerializer, RobustSerializer>();
            IoCManager.Register<IRuntimeLog, RuntimeLog>();
            IoCManager.Register<ITaskManager, TaskManager>();
            IoCManager.Register<TaskManager, TaskManager>();
            IoCManager.Register<ITimerManager, TimerManager>();
            IoCManager.Register<IRobustRandom, RobustRandom>();
            IoCManager.Register<IRobustMappedStringSerializer, RobustMappedStringSerializer>();
            IoCManager.Register<ISandboxHelper, SandboxHelper>();
            IoCManager.Register<IManifoldManager, CollisionManager>();
            IoCManager.Register<IIslandManager, IslandManager>();
            IoCManager.Register<IVerticesSimplifier, RamerDouglasPeuckerSimplifier>();
        }
    }
}
