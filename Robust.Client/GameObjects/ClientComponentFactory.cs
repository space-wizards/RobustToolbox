using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics;

namespace Robust.Client.GameObjects
{
    public class ClientComponentFactory : ComponentFactory
    {
        public ClientComponentFactory()
        {
            // Required for the engine to work
            RegisterIgnore("KeyBindingInput");

            RegisterClass<MetaDataComponent>();
            RegisterClass<TransformComponent>();
            RegisterClass<MapComponent>();
            RegisterClass<MapGridComponent>();
            RegisterClass<PhysicsComponent>();
            RegisterClass<CollisionWakeComponent>();
            RegisterClass<ClientUserInterfaceComponent>();
            RegisterClass<ContainerManagerComponent>();
            RegisterClass<InputComponent>();
            RegisterClass<SpriteComponent>();
            RegisterClass<ClientOccluderComponent>();
            RegisterClass<EyeComponent>();
            RegisterClass<AppearanceComponent>();
            RegisterClass<AppearanceTestComponent>();
            RegisterClass<SnapGridComponent>();
            RegisterClass<AnimationPlayerComponent>();
            RegisterClass<TimerComponent>();

#if DEBUG
            RegisterClass<DebugExceptionOnAddComponent>();
            RegisterClass<DebugExceptionInitializeComponent>();
            RegisterClass<DebugExceptionStartupComponent>();
#endif

        }
    }
}
