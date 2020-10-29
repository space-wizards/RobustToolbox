using System;
using System.Collections.Generic;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Physics
{
    /// <summary>
    /// A physics shape that represents an Axis-Aligned Bounding Box.
    /// This box DOES rotate with the entity, and will always be offset from the
    /// entity origin in world space.
    /// </summary>
    [Serializable, NetSerializable]
    public class PhysShapeRect : IPhysShape
    {
        private int _collisionLayer;
        private int _collisionMask;

        [ViewVariables(VVAccess.ReadWrite)]
        private Dictionary<Angle, Box2> boundingBoxOverrides = new Dictionary<Angle, Box2>();

        //this will result in a incosistency of up to 0,05729578°, which is acceptable imo since we cant expect someone to write down all 100000 decimal places of something in radians
        private Angle GetOverrideIndexAngle(Angle angle) => new Angle(Math.Round(angle.Theta, 3));
        private Box2? GetBoxOverride(Angle angle)
        {
            var roundedAngle = GetOverrideIndexAngle(angle);
            return boundingBoxOverrides.ContainsKey(roundedAngle) ? boundingBoxOverrides[roundedAngle] : (Box2?)null;
        }

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

        public Box2Rotated GetBox2Rotated(Angle angle)
        {
            var roundedAngle = GetOverrideIndexAngle(angle);
            var overrideBB = GetBoxOverride(roundedAngle);
            return overrideBB == null ? new Box2Rotated(_rectangle, angle, Vector2.Zero) : new Box2Rotated(overrideBB.Value, new Angle(), Vector2.Zero);
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
            float sleepPercent, Angle angle)
        {
            handle.SetTransform(modelMatrix);
            handle.DrawRect(GetBox2Rotated(angle.Opposite()), handle.CalcWakeColor(handle.RectFillColor, sleepPercent));
            handle.SetTransform(Matrix3.Identity);
        }

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref _collisionLayer, "layer", 0, WithFormat.Flags<CollisionLayer>());
            serializer.DataField(ref _collisionMask, "mask", 0, WithFormat.Flags<CollisionMask>());
            serializer.DataField(ref _rectangle, "bounds", Box2.UnitCentered);

            List<BoundingBoxOverridePrototype> overrides = new List<BoundingBoxOverridePrototype>();
            serializer.DataField(ref overrides, "overrides", new List<BoundingBoxOverridePrototype>());
            foreach (var boundingBoxOverridePrototype in overrides)
            {
                var roundedAngle = GetOverrideIndexAngle(boundingBoxOverridePrototype.Angle);
                boundingBoxOverrides.Add(roundedAngle, boundingBoxOverridePrototype.Override);
            }
        }

        [field: NonSerialized]
        public event Action? OnDataChanged;

        public Box2 CalculateLocalBounds(Angle rotation)
        {
            return GetBox2Rotated(rotation.Opposite()).CalcBoundingBox();
        }

        private struct BoundingBoxOverridePrototype : IExposeData
        {
            public Box2 Override { get; private set; }
            public Angle Angle { get; private set; }
            public void ExposeData(ObjectSerializer serializer)
            {
                Angle angle = new Angle();
                if (!serializer.TryReadDataField("angle", out angle))
                {
                    var dir = Direction.East;
                    serializer.DataField(ref dir, "angle", Direction.East);
                    angle = dir.ToAngle();
                }
                Angle = angle;

                var bounds = Box2.UnitCentered;
                serializer.DataField(ref bounds, "bounds", Box2.UnitCentered);
                Override = bounds;
            }
        }
    }
}
