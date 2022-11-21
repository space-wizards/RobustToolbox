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
using Robust.Shared.Profiling;
using Robust.Shared.Random;
using Robust.Shared.Sandboxing;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Threading;
using Robust.Shared.Timing;

namespace Robust.Shared
{
    internal static class SharedIoC
    {
        public static void RegisterIoC(IDependencyCollection deps)
        {
            deps.Register<ISerializationManager, SerializationManager>();
            deps.Register<IConfigurationManager, NetConfigurationManager>();
            deps.Register<INetConfigurationManager, NetConfigurationManager>();
            deps.Register<IConfigurationManagerInternal, NetConfigurationManager>();
            deps.Register<INetConfigurationManagerInternal, NetConfigurationManager>();
            deps.Register<IDynamicTypeFactory, DynamicTypeFactory>();
            deps.Register<IDynamicTypeFactoryInternal, DynamicTypeFactory>();
            deps.Register<IEntitySystemManager, EntitySystemManager>();
            deps.Register<ILocalizationManager, LocalizationManager>();
            deps.Register<ILocalizationManagerInternal, LocalizationManager>();
            deps.Register<ILogManager, LogManager>();
            deps.Register<IModLoader, ModLoader>();
            deps.Register<IModLoaderInternal, ModLoader>();
            deps.Register<INetManager, NetManager>();
            deps.Register<IRobustSerializer, RobustSerializer>();
            deps.Register<IRuntimeLog, RuntimeLog>();
            deps.Register<ITaskManager, TaskManager>();
            deps.Register<TaskManager, TaskManager>();
            deps.Register<ITimerManager, TimerManager>();
            deps.Register<ProfManager, ProfManager>();
            deps.Register<IRobustRandom, RobustRandom>();
            deps.Register<IRobustMappedStringSerializer, RobustMappedStringSerializer>();
            deps.Register<ISandboxHelper, SandboxHelper>();
            deps.Register<IManifoldManager, CollisionManager>();
            deps.Register<IIslandManager, IslandManager>();
            deps.Register<IVerticesSimplifier, RamerDouglasPeuckerSimplifier>();
            deps.Register<IParallelManager, ParallelManager>();
            deps.Register<IParallelManagerInternal, ParallelManager>();
        }
    }
}
