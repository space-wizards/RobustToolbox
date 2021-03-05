using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Physics.Dynamics.Shapes
{
    /// <summary>
    /// A physics shape that represents an Axis-Aligned Bounding Box.
    /// This box does not rotate with the entity, and will always be offset from the
    /// entity origin in world space.
    /// </summary>
    [Serializable, NetSerializable]
    [DataDefinition]
    public class PhysShapeAabb : IPhysShape
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
            }
        }

        private float _radius = IoCManager.Resolve<IConfigurationManager>().GetCVar(CVars.PolygonRadius);

        public ShapeType ShapeType => ShapeType.Aabb;

        [DataField("bounds")]
        private Box2 _localBounds = Box2.UnitCentered;

        /// <summary>
        /// Local AABB bounds of this shape.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public Box2 LocalBounds
        {
            get => _localBounds;
            set
            {
                if (_localBounds == value)
                    return;

                _localBounds = value;
                OnDataChanged?.Invoke();
            }
        }

        /// <inheritdoc />
        public void ApplyState() { }

        public void DebugDraw(DebugDrawingHandle handle, in Matrix3 modelMatrix, in Box2 worldViewport,
            float sleepPercent)
        {
            var m = Matrix3.Identity;
            m.R0C2 = modelMatrix.R0C2;
            m.R1C2 = modelMatrix.R1C2;

            handle.SetTransform(m);
            handle.DrawRect(LocalBounds, handle.CalcWakeColor(handle.RectFillColor, sleepPercent));
            handle.SetTransform(Matrix3.Identity);
        }

        // TODO
        [field: NonSerialized]
        public event Action? OnDataChanged;

        /// <inheritdoc />
        public Box2 CalculateLocalBounds(Angle rotation)
        {
            // TODO: Make a new ComputeAABB func or just wrap ComputeAABB into the existing methods?
            return _localBounds.Scale(1 + Radius);
        }

        [Pure]
        internal List<Vector2> GetVertices()
        {
            return new()
            {
                _localBounds.BottomRight,
                _localBounds.TopRight,
                _localBounds.TopLeft,
                _localBounds.BottomLeft,
            };
        }

        public bool Equals(IPhysShape? other)
        {
            if (other is not PhysShapeAabb otherAABB) return false;
            return _localBounds.EqualsApprox(otherAABB._localBounds);
        }
    }
}
