using System;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Physics.Collision.Shapes
{
    /// <summary>
    /// A physics shape that represents an OBB.
    /// This box DOES rotate with the entity, and will always be offset from the
    /// entity origin in world space.
    /// </summary>
    [Serializable, NetSerializable]
    [DataDefinition]
    public class PhysShapeRect : IPhysShape
    {
        public int ChildCount => 1;

        /// <summary>
        /// The radius of this AABB
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
                // ComputeProperties();
            }
        }

        private float _radius = IoCManager.Resolve<IConfigurationManager>().GetCVar(CVars.PolygonRadius);

        internal Vector2 Centroid { get; set; } = Vector2.Zero;

        public ShapeType ShapeType => ShapeType.Rectangle;

        /// <summary>
        ///     The actual bounds of the rectangle. You probably want to use CachedBounds as it has the rotation applied.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("bounds")]
        internal Box2 Rectangle = Box2.UnitCentered;

        public Box2 CachedBounds => _cachedBounds;

        [ViewVariables]
        private Box2 _cachedBounds;

        /// <inheritdoc />
        public void ApplyState() { }

        public void DebugDraw(DebugDrawingHandle handle, in Matrix3 modelMatrix, in Box2 worldViewport,
            float sleepPercent)
        {
            var rotationMatrix = Matrix3.CreateRotation(Math.PI);
            handle.SetTransform(rotationMatrix * modelMatrix);
            handle.DrawRect(Rectangle, handle.CalcWakeColor(handle.RectFillColor, sleepPercent));
        }

        [field: NonSerialized]
        public event Action? OnDataChanged;

        public Box2 CalculateLocalBounds(Angle rotation)
        {
            _cachedBounds = new Box2Rotated(Rectangle, rotation.Opposite(), Vector2.Zero).CalcBoundingBox().Scale(1 + Radius);
            return _cachedBounds;
        }

        public bool Equals(IPhysShape? other)
        {
            if (other is not PhysShapeRect rect) return false;
            return Rectangle.EqualsApprox(rect.Rectangle);
        }
    }
}
