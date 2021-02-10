using System;
using System.Collections.Generic;
using Robust.Client.Graphics;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Graphics.Interfaces.Graphics.ClientEye;
using Robust.Client.Graphics.Interfaces.Graphics.Overlays;
using Robust.Client.Graphics.Overlays;
using Robust.Client.Graphics.Shaders;
using Robust.Client.Input;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;

namespace Robust.Client.Debugging
{
    /// <inheritdoc />
    public class DebugDrawing : IDebugDrawing
    {
        [Dependency] private readonly IOverlayManager _overlayManager = default!;
        [Dependency] private readonly IComponentManager _componentManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IPhysicsManager _physicsManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;

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
                    _overlayManager.AddOverlay(new PhysicsOverlay(_componentManager, _eyeManager,
                        _prototypeManager, _inputManager, _physicsManager));
                }
                else
                {
                    _overlayManager.RemoveOverlay(nameof(PhysicsOverlay));
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

        private class PhysicsOverlay : Overlay
        {
            private readonly IComponentManager _componentManager;
            private readonly IEyeManager _eyeManager;
            private readonly IInputManager _inputManager;
            private readonly IPhysicsManager _physicsManager;

            public override OverlaySpace Space => OverlaySpace.WorldSpace | OverlaySpace.ScreenSpace;
            private readonly ShaderInstance _shader;
            private readonly Font _font;

            private Vector2 _hoverStartScreen = Vector2.Zero;
            private List<IPhysBody> _hoverBodies = new();

            public PhysicsOverlay(IComponentManager compMan, IEyeManager eyeMan, IPrototypeManager protoMan, IInputManager inputManager, IPhysicsManager physicsManager)
                : base(nameof(PhysicsOverlay))
            {
                _componentManager = compMan;
                _eyeManager = eyeMan;
                _inputManager = inputManager;
                _physicsManager = physicsManager;

                _shader = protoMan.Index<ShaderPrototype>("unshaded").Instance();
                var cache = IoCManager.Resolve<IResourceCache>();
                _font = new VectorFont(cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"), 10);
            }

            /// <inheritdoc />
            protected override void Draw(DrawingHandleBase handle, OverlaySpace currentSpace)
            {
                switch (currentSpace)
                {
                    case OverlaySpace.ScreenSpace:
                        DrawScreen((DrawingHandleScreen) handle);
                        break;
                    case OverlaySpace.WorldSpace:
                        DrawWorld((DrawingHandleWorld) handle);
                        break;
                }

            }

            private void DrawScreen(DrawingHandleScreen screenHandle)
            {
                var lineHeight = _font.GetLineHeight(1f);
                Vector2 drawPos = _hoverStartScreen + new Vector2(20, 0) + new Vector2(0, -(_hoverBodies.Count * 4 * lineHeight / 2f));
                int row = 0;

                foreach (var body in _hoverBodies)
                {
                    if (body != _hoverBodies[0])
                    {
                        DrawString(screenHandle, _font, drawPos + new Vector2(0, row * lineHeight), "------");
                        row++;
                    }

                    DrawString(screenHandle, _font, drawPos + new Vector2(0, row * lineHeight), $"Ent: {body.Entity}");
                    row++;
                    DrawString(screenHandle, _font, drawPos + new Vector2(0, row * lineHeight), $"Layer: {Convert.ToString(body.CollisionLayer, 2)}");
                    row++;
                    DrawString(screenHandle, _font, drawPos + new Vector2(0, row * lineHeight), $"Mask: {Convert.ToString(body.CollisionMask, 2)}");
                    row++;
                    DrawString(screenHandle, _font, drawPos + new Vector2(0, row * lineHeight), $"Enabled: {body.CanCollide}, Hard: {body.Hard}, Anchored: {((IPhysicsComponent)body).Anchored}");
                    row++;
                }

            }

            private void DrawWorld(DrawingHandleWorld worldHandle)
            {
                worldHandle.UseShader(_shader);
                var drawing = new PhysDrawingAdapter(worldHandle);

                _hoverBodies.Clear();
                var mouseScreenPos = _inputManager.MouseScreenPosition;
                var mouseWorldPos = _eyeManager.ScreenToMap(mouseScreenPos).Position;
                _hoverStartScreen = mouseScreenPos;

                var viewport = _eyeManager.GetWorldViewport();

                if (viewport.IsEmpty()) return;

                var mapId = _eyeManager.CurrentMap;

                foreach (var physBody in _physicsManager.GetCollidingEntities(mapId, viewport))
                {
                    // all entities have a TransformComponent
                    var transform = physBody.Entity.Transform;

                    var worldBox = physBody.WorldAABB;
                    if (worldBox.IsEmpty()) continue;

                    var colorEdge = Color.Red.WithAlpha(0.33f);

                    foreach (var shape in physBody.PhysicsShapes)
                    {
                        shape.DebugDraw(drawing, transform.WorldMatrix, in viewport, physBody.SleepAccumulator / (float) physBody.SleepThreshold);
                    }

                    if (worldBox.Contains(mouseWorldPos))
                    {
                        _hoverBodies.Add(physBody);
                    }

                    // draw AABB
                    worldHandle.DrawRect(worldBox, colorEdge, false);
                }
            }

            private static void DrawString(DrawingHandleScreen handle, Font font, Vector2 pos, string str)
            {
                var baseLine = new Vector2(pos.X, font.GetAscent(1) + pos.Y);

                foreach (var chr in str)
                {
                    var advance = font.DrawChar(handle, chr, baseLine, 1, Color.White);
                    baseLine += new Vector2(advance, 0);
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
                    var percent = MathHelper.Clamp(wakePercent, 0, 1);

                    var r = 1 - (percent * (1 - color.R));
                    var g = 1 - (percent * (1 - color.G));
                    var b = 1 - (percent * (1 - color.B));

                    return new Color(r, g, b, color.A);
                }

                public override void DrawRect(in Box2 box, in Color color)
                {
                    _handle.DrawRect(box, color);
                }

                public override void DrawRect(in Box2Rotated box, in Color color)
                {
                    _handle.DrawRect(box, color);
                }

                public override void DrawCircle(Vector2 origin, float radius, in Color color)
                {
                    _handle.DrawCircle(origin, radius, color);
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

            protected override void Draw(DrawingHandleBase handle, OverlaySpace currentSpace)
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
