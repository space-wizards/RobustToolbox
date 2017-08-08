using SS14.Client.Interfaces.GameObjects;
using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using System.Collections.Generic;

namespace SS14.Client.GameObjects
{
    public class ClientComponentFactory : ComponentFactory
    {
        public ClientComponentFactory()
        {
            Register<CollidableComponent>();
            Register<TriggerableComponent>();
            Register<IconComponent>();
            Register<ContextMenuComponent>();
            Register<KeyBindingInputComponent>();
            Register<PointLightComponent>();
            Register<PhysicsComponent>();
            Register<ColliderComponent>();
            Register<TransformComponent>();
            RegisterReference<TransformComponent, ITransformComponent>();
            RegisterReference<TransformComponent, IClientTransformComponent>();

            Register<DirectionComponent>();
            RegisterReference<DirectionComponent, IDirectionComponent>();

            Register<BasicMoverComponent>();
            RegisterReference<BasicMoverComponent, IMoverComponent>();

            Register<SlaveMoverComponent>();
            RegisterReference<SlaveMoverComponent, IMoverComponent>();

            Register<PlayerInputMoverComponent>();
            RegisterReference<PlayerInputMoverComponent, IMoverComponent>();

            Register<BoundingBoxComponent>();

            Register<AnimatedSpriteComponent>();
            RegisterReference<AnimatedSpriteComponent, IClickTargetComponent>();
            RegisterReference<AnimatedSpriteComponent, ISpriteRenderableComponent>();

            Register<WearableAnimatedSpriteComponent>();
            RegisterReference<WearableAnimatedSpriteComponent, IClickTargetComponent>();
            RegisterReference<WearableAnimatedSpriteComponent, ISpriteRenderableComponent>();

            Register<SpriteComponent>();
            RegisterReference<SpriteComponent, ISpriteComponent>();
            RegisterReference<SpriteComponent, IClickTargetComponent>();
            RegisterReference<SpriteComponent, ISpriteRenderableComponent>();

            Register<ItemSpriteComponent>();
            RegisterReference<ItemSpriteComponent, ISpriteComponent>();
            RegisterReference<ItemSpriteComponent, IClickTargetComponent>();
            RegisterReference<ItemSpriteComponent, ISpriteRenderableComponent>();

            Register<MobSpriteComponent>();
            RegisterReference<MobSpriteComponent, ISpriteComponent>();
            RegisterReference<MobSpriteComponent, IClickTargetComponent>();
            RegisterReference<MobSpriteComponent, ISpriteRenderableComponent>();

            Register<ParticleSystemComponent>();
            RegisterReference<ParticleSystemComponent, IParticleSystemComponent>();

            Register<ClickableComponent>();
            RegisterReference<ClickableComponent, IClientClickableComponent>();
            RegisterReference<ClickableComponent, IClickableComponent>();
        }
    }
}
