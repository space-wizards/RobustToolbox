using System.Drawing;
using System.Linq;
using ClientInterfaces.Collision;
using ClientInterfaces.GOC;
using GameObject;
using GorgonLibrary;
using SS13.IoC;
using SS13_Shared.GO;

namespace CGO
{
    public class ColliderComponent : Component
    {
        public Color DebugColor { get; set; }

        private RectangleF AABB
        {
            get
            {
                if (Owner.HasComponent(ComponentFamily.Hitbox))
                    return Owner.GetComponent<HitboxComponent>(ComponentFamily.Hitbox).AABB;
                else if (Owner.HasComponent(ComponentFamily.Renderable))
                    return Owner.GetComponent<IRenderableComponent>(ComponentFamily.Renderable).AverageAABB;
                else
                    return RectangleF.Empty;
            }
        }

        public ColliderComponent()
        {
            Family = ComponentFamily.Collider;
            DebugColor = Color.Lime;
        }

        public RectangleF WorldAABB
        {
            get
            {
                // Return tweaked AABB
                var aabb = AABB;
                var trans = Owner.GetComponent<TransformComponent>(ComponentFamily.Transform);
                if (trans == null)
                    return aabb;
                else if (aabb != null)
                    return new RectangleF(
                        aabb.Left + trans.X,
                        aabb.Top + trans.Y,
                        aabb.Width,
                        aabb.Height);
                else
                    return RectangleF.Empty;
            }
        }

        public bool TryCollision(Vector2D offset, bool bump = false)
        {
            return IoCManager.Resolve<ICollisionManager>().TryCollide(Owner, offset, bump);
        }

        public override void SetParameter(ComponentParameter parameter) {
            switch (parameter.MemberName) {
                case "DebugColor":
                    var color = ColorTranslator.FromHtml(parameter.GetValue<string>());
                    if (!color.IsEmpty)
                        DebugColor = color;
                    break;
            }
        }
    }
}