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
        private RectangleF currentAABB
        {
            get
            {
                if (Owner.HasComponent(ComponentFamily.Hitbox))
                {
                    return Owner.GetComponent<HitboxComponent>(ComponentFamily.Hitbox).AABB;
                }
                if (Owner.HasComponent(ComponentFamily.Renderable))
                {
                    return Owner.GetComponent<IRenderableComponent>(ComponentFamily.Renderable).AverageAABB;
                }
                return RectangleF.Empty;
            }
        }

        /// <summary>
        /// X - Top | Y - Right | Z - Bottom | W - Left
        /// </summary>
        private Vector4D tweakAABB;

        public ColliderComponent()
        {
            Family = ComponentFamily.Collider;
            tweakAABB = new Vector4D(0, 0, 0, 0);
        }

        private Vector4D TweakAABB
        {
            get { return tweakAABB; }
            set { tweakAABB = value; }
        }

        public RectangleF OffsetAABB
        {
            get
            {
                // Return tweaked AABB
                var currAABB = currentAABB;
                if (currAABB != null)
                    return
                        new RectangleF(
                            currAABB.Left +
                            Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position.X -
                            (currAABB.Width / 2) + tweakAABB.W,
                            currAABB.Top +
                            Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position.Y -
                            (currAABB.Height / 2) + tweakAABB.X,
                            currAABB.Width - (tweakAABB.W - tweakAABB.Y),
                            currAABB.Height - (tweakAABB.X - tweakAABB.Z));
                else
                    return RectangleF.Empty;
            }
        }

        public override void SetParameter(ComponentParameter parameter)
        {
            base.SetParameter(parameter);

            switch (parameter.MemberName)
            {
                case "TweakAABB":
                    TweakAABB = parameter.GetValue<Vector4D>();
                    break;
            }
        }

        public bool TryCollision(Vector2D offset, bool bump = false)
        {
            return IoCManager.Resolve<ICollisionManager>().TryCollide(Owner, offset, bump);
        }
    }
}