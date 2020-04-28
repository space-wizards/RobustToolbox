using System;
using System.Collections.Generic;
using Robust.Shared.Physics;
using Robust.Shared.Serialization;

namespace Robust.Shared.GameObjects.Components
{
    [Serializable, NetSerializable]
    public class CollidableComponentState : ComponentState
    {
        public readonly bool CanCollide;
        public readonly bool HardCollidable;
        public readonly bool ScrapingFloor;
        public readonly List<IPhysShape> PhysShapes;

        public CollidableComponentState(bool CanCollide, bool hardCollidable, bool scrapingFloor, List<IPhysShape> physShapes)
            : base(NetIDs.COLLIDABLE)
        {
            CanCollide = CanCollide;
            HardCollidable = hardCollidable;
            ScrapingFloor = scrapingFloor;
            PhysShapes = physShapes;
        }
    }
}
