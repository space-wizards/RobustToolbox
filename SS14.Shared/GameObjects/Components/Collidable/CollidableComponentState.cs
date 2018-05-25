using System;
using SS14.Shared.Serialization;

namespace SS14.Shared.GameObjects
{
    [Serializable, NetSerializable]
    public class CollidableComponentState : ComponentState
    {
        public readonly bool CollisionEnabled;

        public CollidableComponentState(bool collisionEnabled)
            : base(NetIDs.COLLIDABLE)
        {
            CollisionEnabled = collisionEnabled;
        }
    }
}
