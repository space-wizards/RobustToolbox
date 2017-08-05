using System;

namespace SS14.Shared.GameObjects.Components.Collidable
{
    [Serializable]
    public class CollidableComponentState : ComponentState
    {
        public bool CollisionEnabled;

        public CollidableComponentState(bool collisionEnabled)
            : base(NetIDs.COLLIDABLE)
        {
            CollisionEnabled = collisionEnabled;
        }
    }
}
