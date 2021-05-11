using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Robust.Server.GameObjects
{
    public class ServerComponentFactory : ComponentFactory
    {
        public ServerComponentFactory()
        {
            RegisterIgnore("Input");
            RegisterIgnore("AnimationPlayer");

            RegisterClass<MetaDataComponent>();
            RegisterClass<TransformComponent>();
            RegisterClass<MapComponent>();
            RegisterClass<MapGridComponent>();
            RegisterClass<EyeComponent>();
            RegisterClass<BasicActorComponent>();
            RegisterClass<PhysicsComponent>();
            RegisterClass<CollisionWakeComponent>();
            RegisterClass<ContainerManagerComponent>();
            RegisterClass<OccluderComponent>();
            RegisterClass<SpriteComponent>();
            RegisterClass<AppearanceComponent>();
            RegisterClass<SnapGridComponent>();
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
