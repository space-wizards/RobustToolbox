using System;

namespace SS14.Shared.GameObjects
{
    [Serializable]
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
