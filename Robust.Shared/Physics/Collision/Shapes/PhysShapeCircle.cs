using System;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Physics.Collision.Shapes
{
    /// <summary>
    /// A physics shape that represents a circle. The circle cannot be rotated,
    /// and it's origin is always the same as the entity position.
    /// </summary>
    [Serializable, NetSerializable]
    [DataDefinition]
    public sealed class PhysShapeCircle : IPhysShape
    {
        public int ChildCount => 1;

        public ShapeType ShapeType => ShapeType.Circle;

        /// <inheritdoc />
        public Box2 LocalBounds => CalcLocalBounds();

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
                //ComputeProperties(); //TODO: Optimize here
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
                if (MathHelper.CloseToPercent(_radius, value)) return;
                _radius = value;
                OnDataChanged?.Invoke();
            }
        }

        public float CalculateArea()
        {
            return MathF.PI * _radius * _radius;
        }

        public Box2 ComputeAABB(Transform transform, int childIndex)
        {
            DebugTools.Assert(childIndex == 0);

            var p = transform.Position + Transform.Mul(transform.Quaternion2D, Position);
            return new Box2(p.X - _radius, p.Y - _radius, p.X + _radius, p.Y + _radius);
        }

        private Box2 CalcLocalBounds()
        {
            // circle inscribed in box
            return new Box2(
                _position.X - _radius,
                _position.Y - _radius,
                _position.X + _radius,
                _position.Y + _radius);
        }

        public bool Equals(IPhysShape? other)
        {
            if (other is not PhysShapeCircle otherCircle) return false;
            return MathHelper.CloseToPercent(_radius, otherCircle._radius);
        }
    }
}
