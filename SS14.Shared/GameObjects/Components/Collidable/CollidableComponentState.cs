using System;
using SS14.Shared.Physics;
using SS14.Shared.Serialization;

namespace SS14.Shared.GameObjects
{
    [Serializable, NetSerializable]
    public class CollidableComponentState : ComponentState
    {
        public readonly bool CollisionEnabled;
        public readonly bool HardCollidable;
        public readonly CollisionGroup CollisionLayer;
        public readonly CollisionGroup CollisionMask;

        public CollidableComponentState(bool collisionEnabled, bool hardCollidable, CollisionGroup collisionLayer, CollisionGroup collisionMask)
            : base(NetIDs.COLLIDABLE)
        {
            CollisionEnabled = collisionEnabled;
            HardCollidable = hardCollidable;
            CollisionLayer = collisionLayer;
            CollisionMask = collisionMask;
        }
    }
}
