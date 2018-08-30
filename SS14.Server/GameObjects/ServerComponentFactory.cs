using SS14.Server.GameObjects.Components;
using SS14.Server.GameObjects.Components.Container;
using SS14.Server.GameObjects.Components.UserInterface;
using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Components.BoundingBox;
using SS14.Shared.GameObjects.Components.Transform;
using SS14.Shared.GameObjects.Components.UserInterface;
using SS14.Shared.Interfaces.GameObjects.Components;

namespace SS14.Server.GameObjects
{
    public class ServerComponentFactory : ComponentFactory
    {
        public ServerComponentFactory()
        {
            RegisterIgnore("Icon");
            RegisterIgnore("Occluder");
            RegisterIgnore("Eye");
            RegisterIgnore("IconSmooth");

            Register<BasicActorComponent>();
            RegisterReference<BasicActorComponent, IActorComponent>();

            Register<CollidableComponent>();
            RegisterReference<CollidableComponent, ICollidableComponent>();
            Register<BoundingBoxComponent>();
            Register<PointLightComponent>();

            RegisterIgnore("Input");

            Register<PlayerInputMoverComponent>();
            RegisterReference<PlayerInputMoverComponent, IMoverComponent>();

            Register<ParticleSystemComponent>();
            Register<PhysicsComponent>();
            Register<SpriteComponent>();
            Register<TransformComponent>();
            RegisterReference<TransformComponent, ITransformComponent>();

            Register<ClickableComponent>();
            RegisterReference<ClickableComponent, IClickableComponent>();

            Register<ContainerManagerComponent>();
            RegisterReference<ContainerManagerComponent, IContainerManager>();

            Register<AiControllerComponent>();
            Register<AppearanceComponent>();
            Register<SnapGridComponent>();

            Register<ServerUserInterfaceComponent>();
            RegisterReference<ServerUserInterfaceComponent, SharedUserInterfaceComponent>();
        }
    }
}
