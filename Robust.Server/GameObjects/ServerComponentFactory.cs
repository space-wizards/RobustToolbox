using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.Components.Markers;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects.Components;

namespace Robust.Server.GameObjects
{
    public class ServerComponentFactory : ComponentFactory
    {
        public ServerComponentFactory()
        {
            RegisterIgnore("Icon");
            RegisterIgnore("Occluder");
            RegisterIgnore("Eye");

            Register<BasicActorComponent>();
            RegisterReference<BasicActorComponent, IActorComponent>();

            Register<CollidableComponent>();
            RegisterReference<CollidableComponent, ICollidableComponent>();
            Register<BoundingBoxComponent>();
            Register<PointLightComponent>();

            RegisterIgnore("Input");

            Register<ParticleSystemComponent>();
            Register<PhysicsComponent>();
            Register<SpriteComponent>();
            Register<TransformComponent>();
            RegisterReference<TransformComponent, ITransformComponent>();

            Register<ClickableComponent>();
            RegisterReference<ClickableComponent, IClickableComponent>();

            Register<ContainerManagerComponent>();
            RegisterReference<ContainerManagerComponent, IContainerManager>();

            Register<AppearanceComponent>();
            Register<SnapGridComponent>();

            Register<ServerUserInterfaceComponent>();
            RegisterReference<ServerUserInterfaceComponent, SharedUserInterfaceComponent>();

            Register<IgnorePauseComponent>();

            RegisterIgnore("AnimationPlayer");
        }
    }
}
