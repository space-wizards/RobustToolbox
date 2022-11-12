using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Physics.Components;
using Robust.Shared.Reflection;

namespace Robust.Client.GameObjects
{
    internal sealed class ClientComponentFactory : ComponentFactory
    {
        public ClientComponentFactory(IDynamicTypeFactoryInternal typeFactory, IReflectionManager reflectionManager)
            : base(typeFactory, reflectionManager)
        {
            // Required for the engine to work
            RegisterIgnore("KeyBindingInput");

            RegisterClass<MetaDataComponent>();
            RegisterClass<TransformComponent>();
            RegisterClass<PhysicsComponent>();
            RegisterClass<CollisionWakeComponent>();
            RegisterClass<ClientUserInterfaceComponent>();
            RegisterClass<ContainerManagerComponent>();
            RegisterClass<InputComponent>();
            RegisterClass<SpriteComponent>();
            RegisterClass<ClientOccluderComponent>();
            RegisterClass<OccluderTreeComponent>();
            RegisterClass<EyeComponent>();
            RegisterClass<AnimationPlayerComponent>();
            RegisterClass<TimerComponent>();
            RegisterClass<DebugExceptionOnAddComponent>();
            RegisterClass<DebugExceptionInitializeComponent>();
            RegisterClass<DebugExceptionStartupComponent>();
        }
    }
}
