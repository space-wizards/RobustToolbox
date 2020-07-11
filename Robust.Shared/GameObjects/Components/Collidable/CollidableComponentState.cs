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
        public readonly bool Hard;

        public CollidableComponentState(bool canCollide, BodyStatus status, List<IPhysShape> physShapes, bool hard)
            : base(NetIDs.COLLIDABLE)
        {
            CanCollide = canCollide;
            Status = status;
            PhysShapes = physShapes;
            Hard = hard;
        }
    }
}
