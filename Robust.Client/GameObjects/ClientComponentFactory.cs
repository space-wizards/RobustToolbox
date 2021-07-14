using Robust.Shared.Console;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;

namespace Robust.Client.GameObjects
{
    internal class ClientComponentFactory : ComponentFactory
    {
        public ClientComponentFactory(IDynamicTypeFactoryInternal typeFactory, IReflectionManager reflectionManager, IConsoleHost conHost)
            : base(typeFactory, reflectionManager, conHost)
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
