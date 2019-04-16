using System;
using Robust.Shared.Serialization;

namespace Robust.Shared.GameObjects
{
    [Serializable, NetSerializable]
    public class CollidableComponentState : ComponentState
    {
        public readonly bool CollisionEnabled;
        public readonly bool HardCollidable;
        public readonly int CollisionLayer;
        public readonly int CollisionMask;

        public CollidableComponentState(bool collisionEnabled, bool hardCollidable, int collisionLayer, int collisionMask)
            : base(NetIDs.COLLIDABLE)
        {
            CollisionEnabled = collisionEnabled;
            HardCollidable = hardCollidable;
            CollisionLayer = collisionLayer;
            CollisionMask = collisionMask;
        }
    }
}
