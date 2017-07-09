using SFML.Graphics;
using SFML.System;
using SS14.Client.Interfaces.Collision;
using SS14.Client.Interfaces.GameObjects;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Utility;
using System.Collections.Generic;
using YamlDotNet.RepresentationModel;

namespace SS14.Client.GameObjects
{
    public class ColliderComponent : ClientComponent
    {
        public override string Name => "Collider";
        public SFML.Graphics.Color DebugColor { get; set; }

        private FloatRect AABB
        {
            get
            {
                if (Owner.HasComponent(ComponentFamily.Hitbox))
                    return Owner.GetComponent<HitboxComponent>(ComponentFamily.Hitbox).AABB;
                else if (Owner.HasComponent(ComponentFamily.Renderable))
                    return Owner.GetComponent<IRenderableComponent>(ComponentFamily.Renderable).AverageAABB;
                else
                    return new FloatRect();
            }
        }

        public ColliderComponent()
        {
            Family = ComponentFamily.Collider;
            DebugColor = Color.Blue;
        }

        public FloatRect WorldAABB
        {
            get
            {
                var trans = Owner.GetComponent<TransformComponent>(ComponentFamily.Transform);
                if (trans == null)
                {
                    return AABB;
                }
                else
                {
                    return new FloatRect(
                        AABB.Left + trans.X,
                        AABB.Top + trans.Y,
                        AABB.Width,
                        AABB.Height);
                }
            }
        }

        public bool TryCollision(Vector2f offset, bool bump = false)
        {
            return IoCManager.Resolve<ICollisionManager>().TryCollide(Owner, offset, bump);
        }

        public override void LoadParameters(YamlMappingNode mapping)
        {
            YamlNode node;
            if (mapping.TryGetNode("debugColor", out node))
            {
                DebugColor = node.AsHexColor(Color.Blue);
            }
        }
    }
}
