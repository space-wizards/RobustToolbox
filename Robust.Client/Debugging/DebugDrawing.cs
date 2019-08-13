using Robust.Client.Graphics.Drawing;
using Robust.Client.Graphics.Overlays;
using Robust.Client.Graphics.Shaders;
using Robust.Client.Interfaces.Debugging;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Client.Interfaces.Graphics.Overlays;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;

namespace Robust.Client.Debugging
{
    /// <inheritdoc />
    public class DebugDrawing : IDebugDrawing
    {
#pragma warning disable 649
        [Dependency] private readonly IOverlayManager _overlayManager;
        [Dependency] private readonly IComponentManager _componentManager;
        [Dependency] private readonly IEyeManager _eyeManager;
        [Dependency] private readonly IPrototypeManager _prototypeManager;
        [Dependency] private readonly IEntityManager _entityManager;
#pragma warning restore 649

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
            private readonly IPrototypeManager _prototypeManager;

            public override OverlaySpace Space => OverlaySpace.WorldSpace;

            public CollidableOverlay(IComponentManager compMan, IEyeManager eyeMan, IPrototypeManager protoMan)
                : base(nameof(CollidableOverlay))
            {
                _componentManager = compMan;
                _eyeManager = eyeMan;
                _prototypeManager = protoMan;

                Shader = _prototypeManager.Index<ShaderPrototype>("unshaded").Instance();
            }

            protected override void Draw(DrawingHandleBase handle)
            {
                var worldHandle = (DrawingHandleWorld) handle;

                var viewport = _eyeManager.GetWorldViewport();
                foreach (var boundingBox in _componentManager.GetAllComponents<ICollidableComponent>())
                {
                    // all entities have a TransformComponent
                    var transform = ((IPhysBody) boundingBox).Owner.Transform;

                    // if not on the same map, continue
                    if (transform.MapID != _eyeManager.CurrentMap || !transform.IsMapTransform)
                        continue;

                    var worldBox = boundingBox.WorldAABB;
                    var colorFill = Color.Green.WithAlpha(0.25f);
                    var colorEdge = Color.Red.WithAlpha(0.33f);

                    // if not on screen, or too small, continue
                    if (!worldBox.Intersects(viewport) || worldBox.IsEmpty())
                        continue;

                    foreach (var shape in boundingBox.PhysicsShapes)
                    {
                        if (shape is PhysShapeAabb aabb)
                        {
                            // TODO: Add a debug drawing function to IPhysShape
                            var shapeWorldBox = aabb.CalculateLocalBounds(transform.WorldRotation).Translated(transform.WorldPosition);
                            worldHandle.DrawRect(shapeWorldBox, colorFill);
                        }
                    }
                    
                    // draw AABB
                    worldHandle.DrawRect(worldBox, colorEdge, false);
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
