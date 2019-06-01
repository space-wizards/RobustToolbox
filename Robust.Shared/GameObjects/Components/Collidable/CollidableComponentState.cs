using System;
using Robust.Shared.Physics;
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
        public readonly bool ScrapingFloor;
        public readonly IPhysShape PhysShape;

        public CollidableComponentState(bool collisionEnabled, bool hardCollidable, int collisionLayer, int collisionMask, bool scrapingFloor, IPhysShape physShape)
            : base(NetIDs.COLLIDABLE)
        {
            CollisionEnabled = collisionEnabled;
            HardCollidable = hardCollidable;
            CollisionLayer = collisionLayer;
            CollisionMask = collisionMask;
            ScrapingFloor = scrapingFloor;
            PhysShape = physShape;
        }
    }
}
