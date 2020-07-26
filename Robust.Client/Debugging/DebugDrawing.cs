using System;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Graphics.Overlays;
using Robust.Client.Graphics.Shaders;
using Robust.Client.Interfaces.Debugging;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Client.Interfaces.Graphics.Overlays;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using MathF = CannyFastMath.MathF;

namespace Robust.Client.Debugging
{
    /// <inheritdoc />
    public class DebugDrawing : IDebugDrawing
    {
        [Dependency] private readonly IOverlayManager _overlayManager = default!;
        [Dependency] private readonly IComponentManager _componentManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        private bool _debugColliders;
        private bool _debugPositions;

        /// <inheritdoc />
        public bool DebugColliders
        {
            get => _debugColliders;
            set
            {
                if (value == DebugColliders)
                {
                    return;
                }

                _debugColliders = value;

                if (value)
                {
                    _overlayManager.AddOverlay(new CollidableOverlay(_componentManager, _eyeManager,
                        _prototypeManager));
                }
                else
                {
                    _overlayManager.RemoveOverlay(nameof(CollidableOverlay));
                }
            }
        }

        /// <inheritdoc />
        public bool DebugPositions
        {
            get => _debugPositions;
            set
            {
                if (value == DebugPositions)
                {
                    return;
                }

                _debugPositions = value;

                if (value)
                {
                    _overlayManager.AddOverlay(new EntityPositionOverlay(_entityManager, _eyeManager));
                }
                else
                {
                    _overlayManager.RemoveOverlay(nameof(EntityPositionOverlay));
                }
            }
        }

        private class CollidableOverlay : Overlay
        {
            private readonly IComponentManager _componentManager;
            private readonly IEyeManager _eyeManager;

            public override OverlaySpace Space => OverlaySpace.WorldSpace;

            public CollidableOverlay(IComponentManager compMan, IEyeManager eyeMan, IPrototypeManager protoMan)
                : base(nameof(CollidableOverlay))
            {
                _componentManager = compMan;
                _eyeManager = eyeMan;

                Shader = protoMan.Index<ShaderPrototype>("unshaded").Instance();
            }

            protected override void Draw(DrawingHandleBase handle)
            {
                var worldHandle = (DrawingHandleWorld) handle;
                var drawing = new PhysDrawingAdapter(worldHandle);

                var viewport = _eyeManager.GetWorldViewport();
                foreach (var boundingBox in _componentManager.GetAllComponents<ICollidableComponent>())
                {
                    var physBody = (IPhysBody)boundingBox;

                    // all entities have a TransformComponent
                    var transform = physBody.Entity.Transform;

                    // if not on the same map, continue
                    if (transform.MapID != _eyeManager.CurrentMap || !transform.IsMapTransform)
                        continue;

                    var worldBox = boundingBox.WorldAABB;
                    var colorEdge = Color.Red.WithAlpha(0.33f);

                    // if not on screen, or too small, continue
                    if (!worldBox.Intersects(viewport) || worldBox.IsEmpty())
                        continue;

                    foreach (var shape in boundingBox.PhysicsShapes)
                    {
                        shape.DebugDraw(drawing, transform.WorldMatrix, in viewport, physBody.SleepAccumulator / (float)physBody.SleepThreshold);
                    }

                    // draw AABB
                    worldHandle.DrawRect(worldBox, colorEdge, false);
                }
            }

            private class PhysDrawingAdapter : DebugDrawingHandle
            {
                private readonly DrawingHandleWorld _handle;

                public PhysDrawingAdapter(DrawingHandleWorld worldHandle)
                {
                    _handle = worldHandle;
                }

                public override Color WakeMixColor => Color.White;
                public override Color GridFillColor => Color.Blue.WithAlpha(0.05f);
                public override Color RectFillColor => Color.Green.WithAlpha(0.25f);

                public override Color CalcWakeColor(Color color, float wakePercent)
                {
                    var percent = MathF.Clamp(wakePercent, 0, 1);

                    var r = 1 - (percent * (1 - color.R));
                    var g = 1 - (percent * (1 - color.G));
                    var b = 1 - (percent * (1 - color.B));

                    return new Color(r, g, b, color.A);
                }

                public override void DrawRect(in Box2 box, in Color color)
                {
                    _handle.DrawRect(box, color);
                }

                public override void SetTransform(in Matrix3 transform)
                {
                    _handle.SetTransform(transform);
                }
            }
        }

        private sealed class EntityPositionOverlay : Overlay
        {
            private readonly IEntityManager _entityManager;
            private readonly IEyeManager _eyeManager;

            public override OverlaySpace Space => OverlaySpace.WorldSpace;

            public EntityPositionOverlay(IEntityManager entityManager, IEyeManager eyeManager) : base(nameof(EntityPositionOverlay))
            {
                _entityManager = entityManager;
                _eyeManager = eyeManager;
            }

            protected override void Draw(DrawingHandleBase handle)
            {
                const float stubLength = 0.25f;

                var worldHandle = (DrawingHandleWorld) handle;
                foreach (var entity in _entityManager.GetEntities())
                {
                    var transform = entity.Transform;
                    if (transform.MapID != _eyeManager.CurrentMap ||
                        !_eyeManager.GetWorldViewport().Contains(transform.WorldPosition))
                    {
                        continue;
                    }

                    var center = transform.WorldPosition;
                    var xLine = transform.WorldRotation.RotateVec(Vector2.UnitX);
                    var yLine = transform.WorldRotation.RotateVec(Vector2.UnitY);

                    worldHandle.DrawLine(center, center + xLine * stubLength, Color.Red);
                    worldHandle.DrawLine(center, center + yLine * stubLength, Color.Green);
                }
            }
        }
    }
}
