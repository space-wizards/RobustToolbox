using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using System.Collections.Generic;

namespace SS14.Server.GameObjects
{
    public class ServerComponentFactory : ComponentFactory
    {
        public ServerComponentFactory()
        {
            RegisterIgnore("Icon");

            Register<BasicActorComponent>();
            RegisterReference<BasicActorComponent, IActorComponent>();

            Register<CollidableComponent>();
            Register<DirectionComponent>();
            RegisterReference<DirectionComponent, IDirectionComponent>();

            Register<BoundingBoxComponent>();
            Register<KeyBindingInputComponent>();
            Register<PointLightComponent>();
            Register<BasicMoverComponent>();
            RegisterReference<BasicMoverComponent, IMoverComponent>();

            Register<PlayerInputMoverComponent>();
            RegisterReference<PlayerInputMoverComponent, IMoverComponent>();

            Register<SlaveMoverComponent>();
            RegisterReference<SlaveMoverComponent, IMoverComponent>();

            Register<ParticleSystemComponent>();
            Register<PhysicsComponent>();
            Register<SpriteComponent>();
            Register<AnimatedSpriteComponent>();
            Register<WearableAnimatedSpriteComponent>();
            Register<TransformComponent>();
            RegisterReference<TransformComponent, ITransformComponent>();

            Register<ClickableComponent>();
            RegisterReference<ClickableComponent, IClickableComponent>();
        }
    }
}
