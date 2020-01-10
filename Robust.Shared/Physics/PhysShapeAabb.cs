using System;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Physics
{
    /// <summary>
    /// A physics shape that represents an Axis-Aligned Bounding Box.
    /// This box does not rotate with the entity, and will always be offset from the
    /// entity origin in world space.
    /// </summary>
    [Serializable, NetSerializable]
    public class PhysShapeAabb : IPhysShape
    {
        private int _collisionLayer;
        private int _collisionMask;
        private Box2 _localBounds = Box2.UnitCentered;

        /// <summary>
        /// Local AABB bounds of this shape.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public Box2 LocalBounds
        {
            get => _localBounds;
            set => _localBounds = value;
        }

        /// <inheritdoc />
        [ViewVariables(VVAccess.ReadWrite)]
        public int CollisionLayer
        {
            get => _collisionLayer;
            set => _collisionLayer = value;
        }

        /// <inheritdoc />
        [ViewVariables(VVAccess.ReadWrite)]
        public int CollisionMask
        {
            get => _collisionMask;
            set => _collisionMask = value;
        }

        /// <inheritdoc />
        public void ApplyState() { }

        /// <inheritdoc />
        public Box2 CalculateLocalBounds(Angle rotation)
        {
            return _localBounds;
        }

        /// <inheritdoc />
        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref _collisionLayer, "layer", 0);
            serializer.DataField(ref _collisionMask, "mask", 0);
            serializer.DataField(ref _localBounds, "bounds", Box2.UnitCentered);
        }
    }
}
