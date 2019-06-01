using Robust.Client.GameObjects.Components;
using Robust.Client.GameObjects.Components.Animations;
using Robust.Client.GameObjects.Components.UserInterface;
using Robust.Client.Interfaces.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.Physics;

namespace Robust.Client.GameObjects
{
    public class ClientComponentFactory : ComponentFactory
    {
        public ClientComponentFactory()
        {
            // Required for the engine to work
            Register<MetaDataComponent>();
            RegisterReference<MetaDataComponent, IMetaDataComponent>();

            // Required for the engine to work
            Register<TransformComponent>();
            RegisterReference<TransformComponent, ITransformComponent>();

            Register<CollidableComponent>();
            RegisterReference<CollidableComponent, ICollidable>();
            RegisterReference<CollidableComponent, ICollidableComponent>();
            Register<IconComponent>();
            RegisterIgnore("KeyBindingInput");
            Register<PointLightComponent>();
            Register<PhysicsComponent>();

            Register<InputComponent>();

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
