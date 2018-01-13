using SS14.Client.Interfaces.GameObjects;
using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Client.GameObjects
{
    public class ClientComponentFactory : ComponentFactory
    {
        public ClientComponentFactory()
        {
            Register<CollidableComponent>();
            RegisterReference<CollidableComponent, ICollidableComponent>();
            Register<IconComponent>();
            Register<ContextMenuComponent>();
            Register<KeyBindingInputComponent>();
            Register<PointLightComponent>();
            Register<PhysicsComponent>();
            Register<TransformComponent>();
            RegisterReference<TransformComponent, ITransformComponent>();
            RegisterReference<TransformComponent, IClientTransformComponent>();

            Register<PlayerInputMoverComponent>();
            RegisterReference<PlayerInputMoverComponent, IMoverComponent>();

            Register<BoundingBoxComponent>();

            Register<SpriteComponent>();
            RegisterReference<SpriteComponent, ISpriteComponent>();
            RegisterReference<SpriteComponent, ISpriteRenderableComponent>();

            Register<ClickableComponent>();
            RegisterReference<ClickableComponent, IClientClickableComponent>();
            RegisterReference<ClickableComponent, IClickableComponent>();

            Register<OccluderComponent>();
        }
    }
}
