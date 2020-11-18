using System;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Physics
{
    [Serializable, NetSerializable]
    public class PhysShapeRect : IPhysShape
    {
        private int _collisionLayer;
        private int _collisionMask;
        private Box2 _rectangle = Box2.UnitCentered;

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

        /// <inheritdoc />
        public float MaximumStepDistance => MathF.Min(_rectangle.Width, _rectangle.Height);

        /// <inheritdoc />
        public void ApplyState() { }

        public void DebugDraw(DebugDrawingHandle handle, in Matrix3 modelMatrix, in Box2 worldViewport,
            float sleepPercent)
        {
            handle.SetTransform(modelMatrix);
            handle.DrawRect(Rectangle, handle.CalcWakeColor(handle.RectFillColor, sleepPercent));
            handle.SetTransform(Matrix3.Identity);
        }

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref _collisionLayer, "layer", 0, WithFormat.Flags<CollisionLayer>());
            serializer.DataField(ref _collisionMask, "mask", 0, WithFormat.Flags<CollisionMask>());
            serializer.DataField(ref _rectangle, "bounds", Box2.UnitCentered);
        }

        [field: NonSerialized]
        public event Action? OnDataChanged;

        public Box2 CalculateLocalBounds(Angle rotation)
        {
            var rect = new Box2Rotated(_rectangle, rotation);
            return rect.CalcBoundingBox();
        }
    }
}
