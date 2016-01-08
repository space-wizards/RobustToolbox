using SS14.Client.Interfaces.Collision;
using SS14.Client.Interfaces.GOC;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;
using SS14.Shared.IoC;
using System.Drawing;
using SS14.Shared.Maths;
using SFML.Graphics;
using SFML.System;

namespace SS14.Client.GameObjects
{
    public class ColliderComponent : Component
    {
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
            DebugColor = SFML.Graphics.Color.Blue;
        }

        public FloatRect WorldAABB
        {
            get
            {
                // Return tweaked AABB
                var aabb = AABB;
                var trans = Owner.GetComponent<TransformComponent>(ComponentFamily.Transform);
                if (trans == null)
                    return aabb;
                else if (aabb != null)
                    return new FloatRect(
                        aabb.Left + trans.X,
                        aabb.Top + trans.Y,
                        aabb.Width,
                        aabb.Height);
                else
                    return new FloatRect();
            }
        }

        public bool TryCollision(Vector2f offset, bool bump = false)
        {
            return IoCManager.Resolve<ICollisionManager>().TryCollide(Owner, offset, bump);
        }

        public override void SetParameter(ComponentParameter parameter) {
            switch (parameter.MemberName) {
                case "DebugColor":
                    DebugColor = ColorUtils.FromHex(parameter.GetValue<string>(), SFML.Graphics.Color.Blue);
                    break;
            }
        }
    }
}