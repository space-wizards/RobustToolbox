using Robust.Shared.Console;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;

namespace Robust.Server.GameObjects
{
    internal class ServerComponentFactory : ComponentFactory
    {
        public ServerComponentFactory(IDynamicTypeFactoryInternal typeFactory, IReflectionManager reflectionManager, IConsoleHost conHost)
            : base(typeFactory, reflectionManager, conHost)
        {
            RegisterIgnore("Input");
            RegisterIgnore("AnimationPlayer");

            RegisterClass<MetaDataComponent>();
            RegisterClass<TransformComponent>();
            RegisterClass<MapComponent>();
            RegisterClass<MapGridComponent>();
            RegisterClass<EyeComponent>();
            RegisterClass<ActorComponent>();
            RegisterClass<PhysicsComponent>();
            RegisterClass<CollisionWakeComponent>();
            RegisterClass<ContainerManagerComponent>();
            RegisterClass<OccluderComponent>();
            RegisterClass<OccluderTreeComponent>();
            RegisterClass<SpriteComponent>();
            RegisterClass<ServerUserInterfaceComponent>();
            RegisterClass<TimerComponent>();
            RegisterClass<MapSaveIdComponent>();

#if DEBUG
            RegisterClass<DebugExceptionOnAddComponent>();
            RegisterClass<DebugExceptionInitializeComponent>();
            RegisterClass<DebugExceptionStartupComponent>();
#endif
        }
    }
}
