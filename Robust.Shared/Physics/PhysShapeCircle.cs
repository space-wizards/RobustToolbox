using System;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Physics
{
    [Serializable, NetSerializable]
    internal class PhysShapeCircle : IPhysShape
    {
        private const float DefaultRadius = 0.5f;

        private int _collisionLayer;
        private int _collisionMask;
        private float _radius = DefaultRadius;

        /// <inheritdoc />
        [field: NonSerialized]
        public event Action? OnDataChanged;

        /// <inheritdoc />
        [ViewVariables(VVAccess.ReadWrite)]
        public int CollisionLayer
        {
            get => _collisionLayer;
            set
            {
                _collisionLayer = value;
                OnDataChanged?.Invoke();
            }
        }

        /// <inheritdoc />
        [ViewVariables(VVAccess.ReadWrite)]
        public int CollisionMask
        {
            get => _collisionMask;
            set
            {
                _collisionMask = value;
                OnDataChanged?.Invoke();
            }
        }

        /// <summary>
        /// The radius of this circle.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float Radius
        {
            get => _radius;
            set => _radius = value;
        }

        /// <inheritdoc />
        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref _collisionLayer, "layer", 0, WithFormat.Flags<CollisionLayer>());
            serializer.DataField(ref _collisionMask, "mask", 0, WithFormat.Flags<CollisionMask>());
            serializer.DataField(ref _radius, "radius", DefaultRadius);
        }

        public Box2 CalculateLocalBounds(Angle rotation)
        {
            return new Box2(-_radius, -_radius, _radius, _radius);
        }

        public void ApplyState() { }

        public void DebugDraw(DebugDrawingHandle handle, in Matrix3 modelMatrix, in Box2 worldViewport, float sleepPercent)
        {
            handle.SetTransform(in modelMatrix);
            handle.DrawCircle(Vector2.Zero, _radius, handle.CalcWakeColor(handle.RectFillColor, sleepPercent));
            handle.SetTransform(in Matrix3.Identity);
        }
    }
}
