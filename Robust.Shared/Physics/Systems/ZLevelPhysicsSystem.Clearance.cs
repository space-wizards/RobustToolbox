using System;
using System.Collections.Generic;
using System.Numerics;
using JetBrains.Annotations;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;
using PhysicsTransform = Robust.Shared.Physics.Transform;

namespace Robust.Shared.Physics.Systems;

public sealed partial class ZLevelPhysicsSystem
{
    [Dependency] private IManifoldManager _manifold = default!;
    [Dependency] private EntityQuery<FixturesComponent> _fixturesQuery = default!;
    [Dependency] private EntityQuery<MapGridComponent> _gridQuery = default!;

    private readonly HashSet<FixtureProxy> _clearanceFixtures = new();
    private List<Entity<MapGridComponent>> _clearanceGrids = new();

    /// <summary>
    /// Returns true when actual destination fixtures overlap the body's support footprint at a z-map transition.
    /// </summary>
    [Pure]
    public bool HasTransitionObstruction(EntityUid uid, int offset)
    {
        if (offset == 0 ||
            !_xformQuery.TryComp(uid, out var xform) ||
            xform.MapUid is not { } currentMap ||
            !_zLevels.TryGetMapOffset(currentMap, offset, out var target) ||
            target is not { } targetMap ||
            !_mapQuery.TryComp(targetMap, out var targetMapComp) ||
            !_zLevels.TryGetMapDepth(currentMap, out var currentDepth))
        {
            return false;
        }

        var absoluteContactHeight = _zPositionQuery.TryComp(uid, out var zPosition)
            ? ZLevelProjection.GetAbsoluteZ(currentDepth.Value, zPosition.LocalHeight)
            : currentDepth.Value + (offset > 0 ? 1f : 0f);
        return HasDestinationFixtureObstruction(
            uid,
            xform,
            targetMap,
            targetMapComp.MapId,
            absoluteContactHeight,
            offset);
    }

    /// <summary>
    /// Checks whether a body can pass through the destination fixtures.
    /// </summary>
    [Pure]
    public bool HasTileAbove(EntityUid uid) => HasTransitionObstruction(uid, 1);

    private bool HasDestinationFixtureObstruction(
        EntityUid uid,
        TransformComponent xform,
        EntityUid targetMap,
        MapId targetMapId,
        float absoluteBoundary,
        int direction)
    {
        var footprint = GetFootprint(uid, xform);
        if (direction > 0 && HasBlockingTileSurface(targetMapId, footprint))
            return true;

        _clearanceFixtures.Clear();

        if (_fixturesQuery.TryComp(uid, out var bodyFixtures) && footprint.HasFixtures)
        {
            foreach (var bodyFixture in bodyFixtures.Fixtures.Values)
            {
                if (!bodyFixture.Hard)
                    continue;

                var query = CreateClearanceQuery(bodyFixture);
                for (var child = 0; child < bodyFixture.Shape.ChildCount; child++)
                {
                    _lookup.GetFixturesIntersecting(
                        targetMapId,
                        bodyFixture.Shape,
                        child,
                        footprint.Transform,
                        _clearanceFixtures,
                        query);
                }
            }
        }
        else
        {
            var query = new FixtureQueryArgs(
                new QueryFilter
                {
                    LayerBits = -1,
                    MaskBits = -1,
                    Flags = QueryFlags.Static,
                },
                Approximate: false,
                IgnoreShapeSkin: true);
            _lookup.GetFixturesIntersecting(targetMapId, footprint.Bounds, _clearanceFixtures, query);
        }

        foreach (var fixture in _clearanceFixtures)
        {
            if (!fixture.Fixture.Hard || fixture.Entity == uid || fixture.Xform.MapUid != targetMap)
                continue;

            if (direction > 0)
                return true;

            if (_highGroundQuery.TryComp(fixture.Entity, out var highGround) && highGround.SolidVolume)
            {
                if (TryGetVolumeTopHeight(fixture.Entity, highGround, footprint.Center, out var topHeight) &&
                    absoluteBoundary < topHeight - PositionEpsilon)
                {
                    return true;
                }

                continue;
            }

            // Grid fixtures are horizontal tile surfaces. Crossing down from above does not enter their area.
            if (_gridQuery.HasComp(fixture.Entity))
                continue;

            // Hard destination entities without an explicit top surface occupy the destination area.
            return true;
        }

        return false;
    }

    private bool HasBlockingTileSurface(MapId mapId, in BodyFootprint footprint)
    {
        _clearanceGrids.Clear();
        _map.FindGridsIntersecting(mapId, footprint.Bounds, ref _clearanceGrids, approx: false, includeMap: true);
        _clearanceGrids.Sort(CompareClearanceGrids);

        EntityUid previous = default;
        foreach (var grid in _clearanceGrids)
        {
            if (grid.Owner == previous)
                continue;
            previous = grid.Owner;

            var inverse = _transform.GetInvWorldMatrix(grid.Owner);
            var matrix = _transform.GetWorldMatrix(grid.Owner);
            var localBounds = inverse.TransformBox(footprint.Bounds);
            var tileSize = grid.Comp.TileSize;
            var min = new Vector2i(
                (int) MathF.Floor(localBounds.Left / tileSize),
                (int) MathF.Floor(localBounds.Bottom / tileSize));
            var max = new Vector2i(
                (int) MathF.Floor(localBounds.Right / tileSize),
                (int) MathF.Floor(localBounds.Top / tileSize));

            for (var x = min.X; x <= max.X; x++)
            {
                for (var y = min.Y; y <= max.Y; y++)
                {
                    if (!_map.TryGetTileRef(grid.Owner, grid.Comp, new Vector2i(x, y), out var tile) ||
                        tile.Tile.IsEmpty)
                    {
                        continue;
                    }

                    var tileBounds = new Box2(
                        new Vector2(x * tileSize, y * tileSize),
                        new Vector2((x + 1) * tileSize, (y + 1) * tileSize));
                    var shape = new Robust.Shared.Physics.Shapes.SlimPolygon(tileBounds, matrix, out var worldBounds);
                    if (FootprintOverlapsShape(footprint, shape, PhysicsTransform.Empty, worldBounds))
                        return true;
                }
            }
        }

        return false;
    }

    private static FixtureQueryArgs CreateClearanceQuery(Fixture bodyFixture)
        => new(
            new QueryFilter
            {
                LayerBits = bodyFixture.CollisionLayer,
                MaskBits = bodyFixture.CollisionMask,
                Flags = QueryFlags.Static,
            },
            Approximate: false,
            IgnoreShapeSkin: true);

    private bool TryGetVolumeTopHeight(
        EntityUid provider,
        ZLevelHighGroundComponent highGround,
        Vector2 worldPoint,
        out float absoluteHeight)
    {
        _ = highGround;
        return TrySampleSupportHeight(provider, ZLevelSupportSurface.HighGround, worldPoint, out absoluteHeight);
    }

    [SubscribeLocalEvent]
    private void OnPreventCollide(Entity<ZLevelPhysicsComponent> entity, ref PreventCollideEvent args)
    {
        if (!_highGroundQuery.TryComp(args.OtherEntity, out var highGround) || !highGround.SolidVolume)
            return;

        if (!_zPositionQuery.TryComp(entity.Owner, out var zPosition) ||
            Transform(entity).MapUid is not { } map ||
            !_zLevels.TryGetMapDepth(map, out var depth))
        {
            return;
        }

        var absoluteFeet = ZLevelProjection.GetAbsoluteZ(depth.Value, zPosition.LocalHeight);
        var point = _transform.GetWorldPosition(entity.Owner);
        if (!TryGetVolumeTopHeight(args.OtherEntity, highGround, point, out var topHeight))
            return;

        // Above the top / within one step of it position collision is handled by support state rather than the solid vertical wall.
        // Below this threshold the normal fixture collision blocks entry.
        if (absoluteFeet >= topHeight - _maxStepUp - PositionEpsilon)
            args.Cancelled = true;
    }

    private BodyFootprint GetFootprint(EntityUid uid, TransformComponent xform)
    {
        var center = _transform.GetWorldPosition(xform);
        var bounds = Box2.CenteredAround(center, new Vector2(_fallbackFootprintRadius * 2f));
        var hasFixtures = false;
        var physicsTransform = _physics.GetPhysicsTransform(uid, xform);

        if (_fixturesQuery.TryComp(uid, out var fixtures))
        {
            foreach (var fixture in fixtures.Fixtures.Values)
            {
                if (!fixture.Hard)
                    continue;

                for (var child = 0; child < fixture.Shape.ChildCount; child++)
                {
                    var fixtureBounds = fixture.Shape.ComputeAABB(physicsTransform, child);
                    bounds = hasFixtures ? bounds.Union(fixtureBounds) : fixtureBounds;
                    hasFixtures = true;
                }
            }
        }

        return new BodyFootprint(uid, center, bounds, physicsTransform, hasFixtures);
    }

    private bool FootprintOverlapsShape<TShape>(
        in BodyFootprint footprint,
        TShape shape,
        PhysicsTransform shapeTransform,
        Box2 shapeBounds)
        where TShape : Robust.Shared.Physics.Collision.Shapes.IPhysShape
    {
        if (footprint.HasFixtures && _fixturesQuery.TryComp(footprint.Owner, out var fixtures))
        {
            foreach (var bodyFixture in fixtures.Fixtures.Values)
            {
                if (!bodyFixture.Hard)
                    continue;

                for (var child = 0; child < bodyFixture.Shape.ChildCount; child++)
                {
                    if (_manifold.TestOverlap(
                            bodyFixture.Shape,
                            child,
                            shape,
                            0,
                            footprint.Transform,
                            shapeTransform,
                            ignoreShapeSkin: true))
                    {
                        return true;
                    }
                }
            }
        }

        return !footprint.HasFixtures && footprint.Bounds.Intersects(shapeBounds);
    }

    private static int CompareClearanceGrids(Entity<MapGridComponent> left, Entity<MapGridComponent> right)
        => left.Owner.CompareTo(right.Owner);

    private readonly record struct BodyFootprint(
        EntityUid Owner,
        Vector2 Center,
        Box2 Bounds,
        PhysicsTransform Transform,
        bool HasFixtures);
}
