using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS13_Shared.GO.Component.Collidable
{
    [Serializable]
    public class CollidableComponentState : ComponentState
    {
        public bool CollisionEnabled;
        public CollidableComponentState(bool collisionEnabled)
            :base(ComponentFamily.Collidable)
        {
            CollisionEnabled = collisionEnabled;
        }
    }
}
