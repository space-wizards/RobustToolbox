using System;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Physics.Dynamics.Shapes
{
    /// <summary>
    /// A physics shape that represents an OBB.
    /// This box DOES rotate with the entity, and will always be offset from the
    /// entity origin in world space.
    /// </summary>
    [Serializable, NetSerializable]
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
            }
        }

        private float _radius;

        public ShapeType ShapeType => ShapeType.Polygon;

        private Box2 _rectangle = Box2.UnitCentered;
        [ViewVariables(VVAccess.ReadWrite)]
        public Box2 Rectangle
        {
            get => _rectangle;
            set
            {
                _rectangle = value;
                OnDataChanged?.Invoke();
            }
        }

        /// <inheritdoc />
        public void ApplyState() { }

        public void DebugDraw(DebugDrawingHandle handle, in Matrix3 modelMatrix, in Box2 worldViewport,
            float sleepPercent)
        {
            var rotationMatrix = Matrix3.CreateRotation(Math.PI);
            handle.SetTransform(rotationMatrix * modelMatrix);
            handle.DrawRect(_rectangle, handle.CalcWakeColor(handle.RectFillColor, sleepPercent));
            handle.SetTransform(Matrix3.Identity);
        }

        void IExposeData.ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref _rectangle, "bounds", Box2.UnitCentered);

            _radius = IoCManager.Resolve<IConfigurationManager>().GetCVar(CVars.PolygonRadius);
        }

        [field: NonSerialized]
        public event Action? OnDataChanged;

        public Box2 CalculateLocalBounds(Angle rotation)
        {
            return new Box2Rotated(_rectangle, rotation.Opposite(), Vector2.Zero).CalcBoundingBox().Scale(1 + Radius);
        }

        public bool Equals(IPhysShape? other)
        {
            if (other is not PhysShapeRect rect) return false;
            return _rectangle.EqualsApprox(rect._rectangle);
        }
    }
}
