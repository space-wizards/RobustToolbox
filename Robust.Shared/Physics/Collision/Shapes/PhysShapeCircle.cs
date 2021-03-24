using System;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Physics.Collision.Shapes
{
    /// <summary>
    /// A physics shape that represents a circle. The circle cannot be rotated,
    /// and it's origin is always the same as the entity position.
    /// </summary>
    [Serializable, NetSerializable]
    [DataDefinition]
    public class PhysShapeCircle : IPhysShape
    {
        public int ChildCount => 1;

        /// <summary>
        /// Gets or sets the density.
        /// Changing the density causes a recalculation of shape properties.
        /// </summary>
        public float Density
        {
            get => _density;
            set
            {
                if (MathHelper.CloseTo(value, _density)) return;

                _density = value;
                // TODO: ONCHANGE
                ComputeProperties();
            }
        }

        [DataField("density")]
        private float _density;

        public MassData MassData
        {
            get => _massData;
            private set => _massData = value;
        }

        private MassData _massData;

        public ShapeType ShapeType => ShapeType.Circle;

        private const float DefaultRadius = 0.5f;

        [DataField("radius")]
        private float _radius = DefaultRadius;

        /// <summary>
        /// Get or set the position of the circle
        /// </summary>
        public Vector2 Position
        {
            get => _position;
            set
            {
                _position = value;
                ComputeProperties(); //TODO: Optimize here
            }
        }

        [DataField("position")]
        private Vector2 _position;

        /// <inheritdoc />
        [field: NonSerialized]
        public event Action? OnDataChanged;

        /// <summary>
        /// The radius of this circle.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float Radius
        {
            get => _radius;
            set
            {
                if (MathHelper.CloseTo(_radius, value)) return;
                _radius = value;
                OnDataChanged?.Invoke();
                ComputeProperties();
            }
        }

        /// <inheritdoc />
        public Box2 CalculateLocalBounds(Angle rotation)
        {
            return new(-_radius, -_radius, _radius, _radius);
        }

        public float CalculateArea()
        {
            return MathF.PI * _radius * _radius;
        }

        public void ComputeProperties()
        {
            var area = CalculateArea();
            _massData.Mass = Density * area;
            _massData.Centroid = Position;

            // inertia about the local origin
            _massData.Inertia = MassData.Mass * (0.5f * _radius * _radius + Vector2.Dot(Position, Position));
        }

        /// <inheritdoc />
        public void ApplyState() { }

        /// <inheritdoc />
        public void DebugDraw(DebugDrawingHandle handle, in Matrix3 modelMatrix, in Box2 worldViewport,
            float sleepPercent)
        {
            handle.SetTransform(in modelMatrix);
            handle.DrawCircle(Vector2.Zero, _radius, handle.CalcWakeColor(handle.RectFillColor, sleepPercent));
            handle.SetTransform(in Matrix3.Identity);
        }

        public bool Equals(IPhysShape? other)
        {
            if (other is not PhysShapeCircle otherCircle) return false;
            return MathHelper.CloseTo(_radius, otherCircle._radius);
        }
    }
}
