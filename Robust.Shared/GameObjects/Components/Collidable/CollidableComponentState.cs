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
        public readonly BodyStatus Status;
        public readonly List<IPhysShape> PhysShapes;
        public override uint NetID => NetIDs.COLLIDABLE;

        public CollidableComponentState(bool canCollide, BodyStatus status, List<IPhysShape> physShapes)
        {
            CanCollide = canCollide;
            Status = status;
            PhysShapes = physShapes;
        }
    }
}
