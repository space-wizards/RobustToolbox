using System;

namespace SS13_Shared.GO.Component.Collidable
{
    [Serializable]
    public class CollidableComponentState : ComponentState
    {
        public bool CollisionEnabled;

        public CollidableComponentState(bool collisionEnabled)
            : base(ComponentFamily.Collidable)
        {
            CollisionEnabled = collisionEnabled;
        }
    }
}