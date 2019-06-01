using System;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Physics
{
    [Serializable, NetSerializable]
    class PhysShapeAabb : IPhysShape
    {
        private Box2 _localBounds = Box2.UnitCentered;

        [ViewVariables]
        public Box2 LocalBounds
        {
            get => _localBounds;
            set => _localBounds = value;
        }

        /// <inheritdoc />
        public Box2 CalculateLocalBounds(Angle rotation)
        {
            return _localBounds;
        }

        /// <inheritdoc />
        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref _localBounds, "bounds", Box2.UnitCentered);
        }
    }
}
