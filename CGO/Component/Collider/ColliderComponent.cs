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
                if (Owner.HasComponent(ComponentFamily.Renderable))
                    return Owner.GetComponent<IRenderableComponent>(ComponentFamily.Renderable).AverageAABB;
                return RectangleF.Empty;
            }
        }

        /// <summary>
        /// X - Top | Y - Right | Z - Bottom | W - Left
        /// </summary>
        private Vector4D tweakAABB = Vector4D.Zero;

        public ColliderComponent()
        {
            Family = ComponentFamily.Collider;
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

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type,
                                                             params object[] list)
        {
            ComponentReplyMessage reply = base.RecieveMessage(sender, type, list);

            if (sender == this) //Don't listen to our own messages!
                return ComponentReplyMessage.Empty;

            switch (type)
            {
                case ComponentMessageType.CheckCollision:
                    reply = list.Any() ? CheckCollision((bool) list[0]) : CheckCollision();
                    break;
            }

            return reply;
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

        private ComponentReplyMessage CheckCollision(bool SuppressBump = false)
        {
            bool isColliding = false;
            var collisionManager = IoCManager.Resolve<ICollisionManager>();
            isColliding = collisionManager.TryCollide(Owner);
            return new ComponentReplyMessage(ComponentMessageType.CollisionStatus, isColliding);
        }

        public bool TryCollision(Vector2D offset, bool bump = false)
        {
            return IoCManager.Resolve<ICollisionManager>().TryCollide(Owner, offset, bump);
        }
    }
}