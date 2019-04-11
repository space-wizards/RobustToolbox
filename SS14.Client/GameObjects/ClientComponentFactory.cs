using SS14.Client.GameObjects.Components;
using SS14.Client.GameObjects.Components.Animations;
using SS14.Client.GameObjects.Components.UserInterface;
using SS14.Client.Interfaces.GameObjects;
using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Components.Transform;
using SS14.Shared.GameObjects.Components.UserInterface;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.Physics;

namespace SS14.Client.GameObjects
{
    public class ClientComponentFactory : ComponentFactory
    {
        public ClientComponentFactory()
        {
            Register<CollidableComponent>();
            RegisterReference<CollidableComponent, ICollidable>();
            RegisterReference<CollidableComponent, ICollidableComponent>();
            Register<IconComponent>();
            RegisterIgnore("KeyBindingInput");
            Register<PointLightComponent>();
            Register<PhysicsComponent>();
            Register<TransformComponent>();
            RegisterReference<TransformComponent, ITransformComponent>();

            Register<InputComponent>();

            Register<ClientBoundingBoxComponent>();
            RegisterReference<ClientBoundingBoxComponent, BoundingBoxComponent>();

            Register<SpriteComponent>();
            RegisterReference<SpriteComponent, ISpriteComponent>();
            RegisterReference<SpriteComponent, IClickTargetComponent>();

            Register<ClickableComponent>();
            RegisterReference<ClickableComponent, IClientClickableComponent>();
            RegisterReference<ClickableComponent, IClickableComponent>();

            Register<OccluderComponent>();

            Register<EyeComponent>();

            Register<AppearanceComponent>();
            Register<AppearanceTestComponent>();
            Register<SnapGridComponent>();

            Register<ClientUserInterfaceComponent>();
            RegisterReference<ClientUserInterfaceComponent, SharedUserInterfaceComponent>();

            RegisterIgnore("IgnorePause");

            Register<AnimationPlayerComponent>();
        }
    }
}
