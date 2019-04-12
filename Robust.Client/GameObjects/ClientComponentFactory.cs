using Robust.Client.GameObjects.Components.Animations;
using Robust.Client.GameObjects.Components.Appearance;
using Robust.Client.GameObjects.Components.BoundingBox;
using Robust.Client.GameObjects.Components.Collidable;
using Robust.Client.GameObjects.Components.Eye;
using Robust.Client.GameObjects.Components.Icon;
using Robust.Client.GameObjects.Components.Input;
using Robust.Client.GameObjects.Components.Light;
using Robust.Client.GameObjects.Components.Occluder;
using Robust.Client.GameObjects.Components.Physics;
using Robust.Client.GameObjects.Components.Renderable;
using Robust.Client.GameObjects.Components.UserInterface;
using Robust.Client.Interfaces.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Client.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Interfaces.GameObjects.Components;

namespace Robust.Client.GameObjects
{
    public class ClientComponentFactory : ComponentFactory
    {
        public ClientComponentFactory()
        {
            Register<CollidableComponent>();
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
