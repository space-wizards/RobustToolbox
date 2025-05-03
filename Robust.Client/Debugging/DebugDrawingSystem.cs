using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Components;
using System.Numerics;

namespace Robust.Client.Debugging;

/// <summary>
/// A collection of visual debug overlays for the client game.
/// </summary>
public sealed class DebugDrawingSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private bool _debugPositions;
    private bool _debugRotations;
    private bool _debugVelocities;
    private bool _debugAngularVelocities;

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
                _overlayManager.AddOverlay(new EntityPositionOverlay(_lookup, _transform));
            }
            else
            {
                _overlayManager.RemoveOverlay<EntityPositionOverlay>();
            }
        }
    }

    /// <summary>
    /// Toggles the visual overlay of the rotation for each entity on screen.
    /// </summary>
    public bool DebugRotations
    {
        get => _debugRotations;
        set
        {
            if (value == DebugRotations)
            {
                return;
            }

            _debugRotations = value;

            if (value && !_overlayManager.HasOverlay<EntityRotationOverlay>())
            {
                _overlayManager.AddOverlay(new EntityRotationOverlay(_lookup, _transform));
            }
            else
            {
                _overlayManager.RemoveOverlay<EntityRotationOverlay>();
            }
        }
    }

    /// <summary>
    /// Toggles the visual overlay of the local velocity for each entity on screen.
    /// </summary>
    public bool DebugVelocities
    {
        get => _debugVelocities;
        set
        {
            if (value == DebugVelocities)
            {
                return;
            }

            _debugVelocities = value;

            if (value && !_overlayManager.HasOverlay<EntityVelocityOverlay>())
            {
                _overlayManager.AddOverlay(new EntityVelocityOverlay(EntityManager, _lookup, _transform));
            }
            else
            {
                _overlayManager.RemoveOverlay<EntityVelocityOverlay>();
            }
        }
    }

    /// <summary>
    /// Toggles the visual overlay of the angular velocity for each entity on screen.
    /// </summary>
    public bool DebugAngularVelocities
    {
        get => _debugAngularVelocities;
        set
        {
            if (value == DebugAngularVelocities)
            {
                return;
            }

            _debugAngularVelocities = value;

            if (value && !_overlayManager.HasOverlay<EntityAngularVelocityOverlay>())
            {
                _overlayManager.AddOverlay(new EntityAngularVelocityOverlay(EntityManager, _lookup, _transform));
            }
            else
            {
                _overlayManager.RemoveOverlay<EntityAngularVelocityOverlay>();
            }
        }
    }
    private sealed class EntityPositionOverlay(EntityLookupSystem _lookup, SharedTransformSystem _transform) : Overlay
    {
        public override OverlaySpace Space => OverlaySpace.WorldSpace;

        protected internal override void Draw(in OverlayDrawArgs args)
        {
            const float stubLength = 0.25f;

            var worldHandle = (DrawingHandleWorld) args.DrawingHandle;

            foreach (var uid in _lookup.GetEntitiesIntersecting(args.MapId, args.WorldBounds))
            {
                var (center, worldRotation) = _transform.GetWorldPositionRotation(uid);

                var xLine = worldRotation.RotateVec(Vector2.UnitX);
                var yLine = worldRotation.RotateVec(Vector2.UnitY);

                worldHandle.DrawLine(center, center + xLine * stubLength, Color.Red);
                worldHandle.DrawLine(center, center + yLine * stubLength, Color.Green);
            }
        }
    }

    private sealed class EntityRotationOverlay(EntityLookupSystem _lookup, SharedTransformSystem _transform) : Overlay
    {
        public override OverlaySpace Space => OverlaySpace.WorldSpace;

        protected internal override void Draw(in OverlayDrawArgs args)
        {
            const float stubLength = 0.25f;
            var worldHandle = (DrawingHandleWorld) args.DrawingHandle;

            foreach (var uid in _lookup.GetEntitiesIntersecting(args.MapId, args.WorldBounds))
            {
                var (center, worldRotation) = _transform.GetWorldPositionRotation(uid);

                var drawLine = worldRotation.RotateVec(-Vector2.UnitY);

                worldHandle.DrawLine(center, center + drawLine * stubLength, Color.Red);
            }
        }
    }

    private sealed class EntityVelocityOverlay(IEntityManager _entityManager, EntityLookupSystem _lookup, SharedTransformSystem _transform) : Overlay
    {
        public override OverlaySpace Space => OverlaySpace.WorldSpace;

        protected internal override void Draw(in OverlayDrawArgs args)
        {
            const float multiplier = 0.2f;

            var worldHandle = (DrawingHandleWorld) args.DrawingHandle;

            var physicsQuery = _entityManager.GetEntityQuery<PhysicsComponent>();
            foreach (var uid in _lookup.GetEntitiesIntersecting(args.MapId, args.WorldBounds))
            {
                if(!physicsQuery.TryGetComponent(uid, out var physicsComp))
                    continue;

                var center = _transform.GetWorldPosition(uid);
                var localVelocity = physicsComp.LinearVelocity;

                if (localVelocity != Vector2.Zero)
                    worldHandle.DrawLine(center, center + localVelocity * multiplier, Color.Yellow);
            }
        }
    }

    private sealed class EntityAngularVelocityOverlay(IEntityManager _entityManager, EntityLookupSystem _lookup, SharedTransformSystem _transform) : Overlay
    {
        public override OverlaySpace Space => OverlaySpace.WorldSpace;

        protected internal override void Draw(in OverlayDrawArgs args)
        {
            const float multiplier = (float)(0.2 / (2 * System.Math.PI));

            var worldHandle = (DrawingHandleWorld) args.DrawingHandle;

            var physicsQuery = _entityManager.GetEntityQuery<PhysicsComponent>();
            foreach (var uid in _lookup.GetEntitiesIntersecting(args.MapId, args.WorldBounds))
            {
                if(!physicsQuery.TryGetComponent(uid, out var physicsComp))
                    continue;

                var center = _transform.GetWorldPosition(uid);
                var angularVelocity = physicsComp.AngularVelocity;

                if (angularVelocity != 0.0f)
                    worldHandle.DrawCircle(center, angularVelocity * multiplier, angularVelocity > 0 ? Color.Magenta : Color.Blue, false);
            }
        }
    }
}

