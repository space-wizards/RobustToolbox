using System;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Physics
{
    /// <summary>
    /// A physics shape that represents an OBB.
    /// This box DOES rotate with the entity, and will always be offset from the
    /// entity origin in world space.
    /// </summary>
    [Serializable, NetSerializable]
    public class PhysShapeRect : IPhysShape
    {
        [DataFieldWithFlag("layer", typeof(CollisionLayer))]
        private int _collisionLayer;
        [DataFieldWithFlag("mask", typeof(CollisionMask))]
        private int _collisionMask;

        [DataField("bounds")]
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
        public void ApplyState() { }

        public void DebugDraw(DebugDrawingHandle handle, in Matrix3 modelMatrix, in Box2 worldViewport,
            float sleepPercent)
        {
            var rotationMatrix = Matrix3.CreateRotation(Math.PI);
            handle.SetTransform(rotationMatrix * modelMatrix);
            handle.DrawRect(_rectangle, handle.CalcWakeColor(handle.RectFillColor, sleepPercent));
            handle.SetTransform(Matrix3.Identity);
        }

        [field: NonSerialized]
        public event Action? OnDataChanged;

        public Box2 CalculateLocalBounds(Angle rotation)
        {
            return new Box2Rotated(_rectangle, rotation.Opposite(), Vector2.Zero).CalcBoundingBox();
        }
    }
}
