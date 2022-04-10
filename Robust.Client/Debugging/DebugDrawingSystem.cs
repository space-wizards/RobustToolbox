using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Robust.Client.Debugging
{
    /// <summary>
    /// A collection of visual debug overlays for the client game.
    /// </summary>
    public sealed class DebugDrawingSystem : EntitySystem
    {
        [Dependency] private readonly IOverlayManager _overlayManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;

        private bool _debugPositions;

        /// <summary>
        /// Toggles the visual overlay of the local origin for each entity on screen.
        /// </summary>
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
                    _overlayManager.AddOverlay(new EntityPositionOverlay(_lookup, _eyeManager, _entityManager));
                }
                else
                {
                    _overlayManager.RemoveOverlay<EntityPositionOverlay>();
                }
            }
        }

        private sealed class EntityPositionOverlay : Overlay
        {
            private readonly EntityLookupSystem _lookup;
            private readonly IEyeManager _eyeManager;
            private readonly IEntityManager _entityManager;

            public override OverlaySpace Space => OverlaySpace.WorldSpace;

            public EntityPositionOverlay(EntityLookupSystem lookup, IEyeManager eyeManager, IEntityManager entityManager)
            {
                _lookup = lookup;
                _eyeManager = eyeManager;
                _entityManager = entityManager;
            }

            protected internal override void Draw(in OverlayDrawArgs args)
            {
                const float stubLength = 0.25f;

                var worldHandle = (DrawingHandleWorld) args.DrawingHandle;
                var viewport = _eyeManager.GetWorldViewport();
                var xformQuery = _entityManager.GetEntityQuery<TransformComponent>();

                foreach (var entity in _lookup.GetEntitiesIntersecting(_eyeManager.CurrentMap, viewport))
                {
                    var (center, worldRotation) = xformQuery.GetComponent(entity).GetWorldPositionRotation();

                    var xLine = worldRotation.RotateVec(Vector2.UnitX);
                    var yLine = worldRotation.RotateVec(Vector2.UnitY);

                    worldHandle.DrawLine(center, center + xLine * stubLength, Color.Red);
                    worldHandle.DrawLine(center, center + yLine * stubLength, Color.Green);
                }
            }
        }
    }
}
