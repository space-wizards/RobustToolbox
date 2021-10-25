using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Robust.Client.Debugging
{
    /// <inheritdoc />
    public class DebugDrawing : IDebugDrawing
    {
        [Dependency] private readonly IOverlayManager _overlayManager = default!;
        [Dependency] private readonly IEntityManager _entManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IEntityLookup _lookup = default!;

        private bool _debugPositions;

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

                if (value && !_overlayManager.HasOverlay<EntityPositionOverlay>())
                {
                    var lookup = EntitySystem.Get<QuerySystem>();

                    _overlayManager.AddOverlay(new EntityPositionOverlay(lookup, _entManager, _eyeManager));
                }
                else
                {
                    _overlayManager.RemoveOverlay<EntityPositionOverlay>();
                }
            }
        }

        private sealed class EntityPositionOverlay : Overlay
        {
            private readonly QuerySystem _lookup;
            private readonly IEntityManager _entManager;
            private readonly IEyeManager _eyeManager;

            public override OverlaySpace Space => OverlaySpace.WorldSpace;

            public EntityPositionOverlay(QuerySystem lookup, IEntityManager entManager, IEyeManager eyeManager)
            {
                _lookup = lookup;
                _entManager = entManager;
                _eyeManager = eyeManager;
            }

            protected internal override void Draw(in OverlayDrawArgs args)
            {
                const float stubLength = 0.25f;

                var worldHandle = (DrawingHandleWorld) args.DrawingHandle;
                var viewport = _eyeManager.GetWorldViewport();

                foreach (var entity in _lookup.GetEntitiesIntersecting(_eyeManager.CurrentMap, viewport))
                {
                    var transform = _entManager.GetComponent<TransformComponent>(entity);

                    var center = transform.WorldPosition;
                    var worldRotation = transform.WorldRotation;

                    var xLine = worldRotation.RotateVec(Vector2.UnitX);
                    var yLine = worldRotation.RotateVec(Vector2.UnitY);

                    worldHandle.DrawLine(center, center + xLine * stubLength, Color.Red);
                    worldHandle.DrawLine(center, center + yLine * stubLength, Color.Green);
                }
            }
        }
    }
}
