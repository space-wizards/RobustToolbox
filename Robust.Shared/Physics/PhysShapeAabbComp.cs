using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Robust.Shared.Physics
{
    class PhysShapeAabbComp : IPhysShape
    {
        private Box2 _localBounds = Box2.UnitCentered;

        public IEntity Entity { get; set; }

        public Box2 LocalBounds
        {
            get => _localBounds;
            set => _localBounds = value;
        }

        // parameterless public constructor required for deserialization
        public PhysShapeAabbComp() { }
        public PhysShapeAabbComp(IEntity entity)
        {
            Entity = entity;
        }

        /// <inheritdoc />
        public Box2 CalculateLocalBounds(Angle rotation)
        {
            if (Entity.TryGetComponent<BoundingBoxComponent>(out var boundComp))
            {
                _localBounds = boundComp.AABB;
                return boundComp.AABB;
            }

            return _localBounds;
        }

        /// <inheritdoc />
        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref _localBounds, "bounds", Box2.UnitCentered);
        }
    }
}
