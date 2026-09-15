using System;
using System.Collections.Generic;
using System.Numerics;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Shapes;
using PhysicsTransform = Robust.Shared.Physics.Transform;

namespace Robust.Shared.Physics.Systems;

public sealed partial class ZLevelPhysicsSystem
{
    [Dependency] private ZLevelSupportSystem _support = default!;

    private readonly Dictionary<EntityUid, HashSet<EntityUid>> _supportedBodiesByProvider = new();
    private readonly HashSet<EntityUid> _supportRefreshBodies = new();

    /// <summary>
    /// Recomputes the support state beneath a body's support point.
    /// </summary>
    public void RefreshSupport(Entity<ZLevelPhysicsComponent> entity, bool wake = true)
    {
        if (!CanSimulateBody(entity))
        {
            SleepBody(entity);
            return;
        }

        if (!_xformQuery.TryComp(entity.Owner, out var xform) ||
            xform.MapUid is not { } currentMap ||
            !_zLevels.TryGetMapDepth(currentMap, out var currentDepth))
        {
            return;
        }

        var zPosition = EnsureComp<ZLevelPositionComponent>(entity.Owner);
        var oldGround = entity.Comp.GroundState;
        var currentAbsoluteHeight = ZLevelProjection.GetAbsoluteZ(currentDepth.Value, zPosition.LocalHeight);
        var maxRise = oldGround == ZLevelGroundState.Grounded && entity.Comp.AutoStep
            ? _maxStepUp
            : PositionEpsilon;

        var selected = _support.TryQuerySupport(
            entity,
            _transform.GetWorldPosition(xform),
            currentAbsoluteHeight,
            maxRise,
            out var support);

        if (!selected)
        {
            ApplySupportResult(entity, ZLevelSupportResult.None);
            if (oldGround == ZLevelGroundState.Grounded)
            {
                SetGroundState(entity, ZLevelGroundState.Airborne);
                if (wake)
                    WakeBody(entity);
            }

            return;
        }

        ApplySupportResult(entity, support);

        if (oldGround != ZLevelGroundState.Grounded)
            return;

        var rise = support.AbsoluteHeight - currentAbsoluteHeight;
        var maximumSnapDown = MathF.Min(_maxStepDown, _groundSnapDistance);
        if (rise <= _maxStepUp + PositionEpsilon &&
            rise >= -maximumSnapDown - PositionEpsilon)
        {
            SetLocalHeight(
                (entity.Owner, zPosition),
                ZLevelProjection.GetLocalHeight(support.AbsoluteHeight, currentDepth.Value));
            SetGroundState(entity, ZLevelGroundState.Grounded);
            SetVerticalVelocity(entity, 0f);
            NormalizeEntityMapFromAbsoluteZ(entity, support.AbsoluteHeight);
            return;
        }

        SetGroundState(entity, ZLevelGroundState.Airborne);
        if (wake)
            WakeBody(entity);
    }

    /// <summary>
    /// Applies a selected support result without changing grounded state, velocity, z height, or map parent.
    /// </summary>
    private void ApplySupportResult(Entity<ZLevelPhysicsComponent> entity, ZLevelSupportResult support)
    {
        if (entity.Comp.SupportProvider == support.Provider &&
            entity.Comp.SupportSurface == support.Surface &&
            entity.Comp.SupportHeight.Equals(support.AbsoluteHeight))
        {
            return;
        }

        ReplaceSupportedBodyProvider(entity.Owner, entity.Comp.SupportProvider, support.Provider);
        entity.Comp.SupportProvider = support.Provider;
        entity.Comp.SupportSurface = support.Surface;
        entity.Comp.SupportHeight = support.AbsoluteHeight;
        DirtyFields(
            entity.Owner,
            entity.Comp,
            null,
            nameof(ZLevelPhysicsComponent.SupportProvider),
            nameof(ZLevelPhysicsComponent.SupportSurface),
            nameof(ZLevelPhysicsComponent.SupportHeight));
    }

    private void SetGroundState(Entity<ZLevelPhysicsComponent> entity, ZLevelGroundState state)
    {
        if (entity.Comp.GroundState == state)
            return;

        entity.Comp.GroundState = state;
        DirtyField(entity.Owner, entity.Comp, nameof(ZLevelPhysicsComponent.GroundState));
    }

    private void SetVerticalVelocity(Entity<ZLevelPhysicsComponent> entity, float velocity)
    {
        if (entity.Comp.Velocity.Equals(velocity))
            return;

        entity.Comp.Velocity = velocity;
        DirtyField(entity.Owner, entity.Comp, nameof(ZLevelPhysicsComponent.Velocity));
    }

    [Pure]
    public float GetLocalSupportHeight(Entity<ZLevelPhysicsComponent> entity)
    {
        if (entity.Comp.SupportSurface == ZLevelSupportSurface.None ||
            Transform(entity).MapUid is not { } map ||
            !_zLevels.TryGetMapDepth(map, out var depth))
        {
            return 0f;
        }

        return ZLevelProjection.GetLocalHeight(entity.Comp.SupportHeight, depth.Value);
    }

    /// <summary>
    /// Reparents a grounded body to the z-map that owns its absolute support height.
    /// </summary>
    public bool NormalizeEntityMapFromAbsoluteZ(Entity<ZLevelPhysicsComponent> entity, float absoluteZ)
    {
        if (!float.IsFinite(absoluteZ) ||
            !_xformQuery.TryComp(entity.Owner, out var xform) ||
            xform.MapUid is not { } currentMap ||
            !_zLevels.TryGetMapData(currentMap, out var currentZMap, out var network) ||
            network.SortedZLevels.Count == 0)
        {
            return false;
        }

        var targetDepth = Math.Clamp(
            (int) MathF.Floor(absoluteZ + PositionEpsilon),
            0,
            network.SortedZLevels.Count - 1);
        var offset = targetDepth - currentZMap.Depth;
        if (offset == 0)
            return true;

        if (!TryMovePreservingHeight(entity.Owner, offset, out var targetMap) ||
            targetMap is not { } targetMapUid ||
            !_zLevels.TryGetMapDepth(targetMapUid, out var targetMapDepth) ||
            !_zPositionQuery.TryComp(entity.Owner, out var zPosition))
        {
            return false;
        }

        SetLocalHeight(
            (entity.Owner, zPosition),
            ZLevelProjection.GetLocalHeight(absoluteZ, targetMapDepth.Value));
        var moved = new ZLevelMapMoveEvent(offset, targetMapUid);
        RaiseLocalEvent(entity.Owner, ref moved);
        return true;
    }

    /// <summary>
    /// Replaces an anchored flat support height and refreshes nearby/supported bodies.
    /// </summary>
    public void SetSupportHeight(Entity<ZLevelHighGroundComponent> entity, float height)
    {
        if (!float.IsFinite(height) || entity.Comp.Height.Equals(height))
            return;

        entity.Comp.Height = height;
        DirtyField(entity.Owner, entity.Comp, nameof(ZLevelHighGroundComponent.Height));
        RefreshBodiesAtHighGround(entity.Owner);
    }

    public bool TrySampleSupportHeight(
        EntityUid provider,
        ZLevelSupportSurface surface,
        Vector2 worldPoint,
        out float absoluteHeight)
        => _support.TrySampleSurfaceHeight(provider, surface, worldPoint, out absoluteHeight);

    internal void RefreshSupportsOnMovedGrid(EntityUid grid)
    {
        _supportRefreshBodies.Clear();
        AddSupportedBodies(grid, _supportRefreshBodies);

        foreach (var provider in _supportedBodiesByProvider.Keys)
        {
            if (provider == grid ||
                !_xformQuery.TryComp(provider, out var providerXform) ||
                providerXform.GridUid != grid)
            {
                continue;
            }

            AddSupportedBodies(provider, _supportRefreshBodies);
        }

        // Refresh after collecting because support can change here.
        RefreshSupportBodySet(wake: false);
    }

    private void RefreshSupportedBodiesAtProvider(EntityUid provider)
    {
        _supportRefreshBodies.Clear();
        AddSupportedBodies(provider, _supportRefreshBodies);
        RefreshSupportBodySet(wake: true);
    }

    private void RefreshSupportBodySet(bool wake)
    {
        foreach (var uid in _supportRefreshBodies)
        {
            if (!_zPhysicsQuery.TryComp(uid, out var physics) || !CanSimulateBody((uid, physics)))
                continue;

            RefreshSupport((uid, physics), wake);
            if (wake)
                WakeBody((uid, physics));
        }

        _supportRefreshBodies.Clear();
    }

    private void AddSupportedBodies(EntityUid provider, HashSet<EntityUid> bodies)
    {
        if (!_supportedBodiesByProvider.TryGetValue(provider, out var supported))
            return;

        foreach (var body in supported)
            bodies.Add(body);
    }

    private void ReplaceSupportedBodyProvider(EntityUid body, EntityUid? oldProvider, EntityUid? newProvider)
    {
        if (oldProvider == newProvider)
            return;

        if (oldProvider is { } oldUid &&
            _supportedBodiesByProvider.TryGetValue(oldUid, out var oldBodies))
        {
            oldBodies.Remove(body);
            if (oldBodies.Count == 0)
                _supportedBodiesByProvider.Remove(oldUid);
        }

        if (newProvider is not { } newUid)
            return;

        if (!_supportedBodiesByProvider.TryGetValue(newUid, out var newBodies))
        {
            newBodies = new HashSet<EntityUid>();
            _supportedBodiesByProvider.Add(newUid, newBodies);
        }

        newBodies.Add(body);
    }

    private void ClearSupportTracking(Entity<ZLevelPhysicsComponent> entity)
    {
        ReplaceSupportedBodyProvider(entity.Owner, entity.Comp.SupportProvider, null);
    }
}

/// <summary>
/// Pure support-surface queries for flat z-level floors, platforms, and wall tops.
/// </summary>
public sealed partial class ZLevelSupportSystem : EntitySystem
{
    private const float PositionEpsilon = 0.00001f;

    [Dependency] private IConfigurationManager _configuration = default!;
    [Dependency] private IManifoldManager _manifold = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private ZLevelSystem _zLevels = default!;

    [Dependency] private EntityQuery<FixturesComponent> _fixturesQuery = default!;
    [Dependency] private EntityQuery<MapComponent> _mapQuery = default!;
    [Dependency] private EntityQuery<MapGridComponent> _gridQuery = default!;
    [Dependency] private EntityQuery<TransformComponent> _xformQuery = default!;
    [Dependency] private EntityQuery<ZLevelHighGroundComponent> _highGroundQuery = default!;
    [Dependency] private EntityQuery<ZLevelMapComponent> _zMapQuery = default!;

    private PhysShapeCircle _supportProbe = new();
    private float _supportBuffer;
    private readonly HashSet<Entity<ZLevelHighGroundComponent>> _supportProviders = new();
    private List<Entity<MapGridComponent>> _supportGrids = new();

    public override void Initialize()
    {
        base.Initialize();
        Subs.CVar(_configuration, CVars.ZLevelSupportBuffer, SetSupportBuffer, true);
    }

    private void SetSupportBuffer(float value)
    {
        _supportBuffer = MathF.Max(0f, value);
        _supportProbe = new PhysShapeCircle(_supportBuffer);
    }

    [Pure]
    public bool TryQuerySupport(
        Entity<ZLevelPhysicsComponent> body,
        Vector2 proposedWorldPosition,
        float currentAbsoluteHeight,
        float maximumRise,
        out ZLevelSupportResult selected)
    {
        selected = ZLevelSupportResult.None;

        if (!float.IsFinite(currentAbsoluteHeight) ||
            !_xformQuery.TryComp(body.Owner, out var xform) ||
            xform.MapUid is not { } currentMap ||
            !_zMapQuery.TryComp(currentMap, out _) ||
            !_mapQuery.TryComp(currentMap, out _))
        {
            return false;
        }

        maximumRise = MathF.Max(0f, maximumRise);
        var sample = new SupportSample(
            proposedWorldPosition,
            Box2.CenteredAround(proposedWorldPosition, new Vector2(_supportBuffer * 2f)),
            new PhysicsTransform(proposedWorldPosition, Angle.Zero));

        for (var floor = 0; floor <= 1; floor++)
        {
            var checkingMap = currentMap;
            if (floor != 0)
            {
                if (!_zLevels.TryGetMapOffset(currentMap, -floor, out var below) || below is not { } belowMap)
                    continue;

                checkingMap = belowMap;
            }

            if (!_zMapQuery.TryComp(checkingMap, out var checkingZMap) ||
                !_mapQuery.TryComp(checkingMap, out var checkingMapComp))
            {
                continue;
            }

            SelectHighGroundSupport(
                body.Owner,
                checkingMap,
                checkingMapComp.MapId,
                checkingZMap.Depth,
                sample,
                currentAbsoluteHeight,
                maximumRise,
                ref selected);
            SelectTileSupport(
                checkingMapComp.MapId,
                checkingZMap.Depth,
                sample,
                currentAbsoluteHeight,
                maximumRise,
                ref selected);
        }

        return selected.Surface != ZLevelSupportSurface.None;
    }

    [Pure]
    public bool TrySampleSurfaceHeight(
        EntityUid provider,
        ZLevelSupportSurface surface,
        Vector2 worldPoint,
        out float absoluteHeight)
    {
        absoluteHeight = 0f;
        switch (surface)
        {
            case ZLevelSupportSurface.Tile:
                if (!_gridQuery.TryComp(provider, out _) ||
                    !_xformQuery.TryComp(provider, out var gridXform) ||
                    gridXform.MapUid is not { } gridMap ||
                    !_zLevels.TryGetMapDepth(gridMap, out var gridDepth))
                {
                    return false;
                }

                absoluteHeight = gridDepth.Value;
                return true;
            case ZLevelSupportSurface.HighGround:
                if (!_highGroundQuery.TryComp(provider, out var highGround) ||
                    !_xformQuery.TryComp(provider, out var xform) ||
                    xform.MapUid is not { } map ||
                    !_zLevels.TryGetMapDepth(map, out var depth) ||
                    !float.IsFinite(highGround.Height))
                {
                    return false;
                }

                absoluteHeight = depth.Value + highGround.Height;
                return true;
            case ZLevelSupportSurface.NetworkBoundary:
                if (!_zLevels.TryGetMapDepth(provider, out var mapDepth))
                    return false;

                absoluteHeight = mapDepth.Value;
                return true;
            default:
                return false;
        }
    }

    private void SelectHighGroundSupport(
        EntityUid body,
        EntityUid checkingMap,
        MapId mapId,
        int mapDepth,
        in SupportSample sample,
        float currentAbsoluteHeight,
        float maximumRise,
        ref ZLevelSupportResult selected)
    {
        _supportProviders.Clear();
        _lookup.GetEntitiesIntersecting(
            mapId,
            sample.Bounds,
            _supportProviders,
            LookupFlags.Uncontained);

        foreach (var provider in _supportProviders)
        {
            if (provider.Owner == body)
            {
                continue;
            }

            if (!_xformQuery.TryComp(provider.Owner, out var providerXform) || providerXform.MapUid != checkingMap)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(provider.Comp.SurfaceFixture))
            {
                continue;
            }

            if (!TryGetSupportFixture(provider, out var fixture))
            {
                continue;
            }

            var height = provider.Comp.Height;
            if (!float.IsFinite(height))
            {
                continue;
            }

            if (!SupportSampleOverlapsFixture(sample, provider.Owner, providerXform, fixture))
            {
                continue;
            }

            var absoluteHeight = mapDepth + height;
            if (absoluteHeight > currentAbsoluteHeight + maximumRise + PositionEpsilon)
            {
                continue;
            }

            SelectBetterSupport(ref selected, new ZLevelSupportResult(
                provider.Owner,
                ZLevelSupportSurface.HighGround,
                absoluteHeight,
                sample.Point));
        }
    }

    private void SelectTileSupport(
        MapId mapId,
        int mapDepth,
        in SupportSample sample,
        float currentAbsoluteHeight,
        float maximumRise,
        ref ZLevelSupportResult selected)
    {
        if (mapDepth > currentAbsoluteHeight + maximumRise + PositionEpsilon)
            return;

        _supportGrids.Clear();
        _map.FindGridsIntersecting(mapId, sample.Bounds, ref _supportGrids, approx: false, includeMap: true);

        foreach (var grid in _supportGrids)
        {
            if (TileSupportOverlapsSample(grid, sample))
            {
                SelectBetterSupport(ref selected, new ZLevelSupportResult(
                    grid.Owner,
                    ZLevelSupportSurface.Tile,
                    mapDepth,
                    sample.Point));
            }
        }
    }

    private bool TileSupportOverlapsSample(Entity<MapGridComponent> grid, in SupportSample sample)
    {
        var inverse = _transform.GetInvWorldMatrix(grid.Owner);
        var matrix = _transform.GetWorldMatrix(grid.Owner);
        var localBounds = inverse.TransformBox(sample.Bounds);
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
                var tileIndex = new Vector2i(x, y);
                if (!_map.TryGetTileRef(grid.Owner, grid.Comp, tileIndex, out var tile) || tile.Tile.IsEmpty)
                    continue;

                var tileBounds = new Box2(
                    new Vector2(x * tileSize, y * tileSize),
                    new Vector2((x + 1) * tileSize, (y + 1) * tileSize));
                var tileShape = new SlimPolygon(tileBounds, matrix, out var tileWorldBounds);
                if (!tileWorldBounds.Intersects(sample.Bounds))
                    continue;

                if (_manifold.TestOverlap(
                        _supportProbe,
                        0,
                        tileShape,
                        0,
                        sample.Transform,
                        PhysicsTransform.Empty,
                        ignoreShapeSkin: true))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool SupportSampleOverlapsFixture(
        in SupportSample sample,
        EntityUid provider,
        TransformComponent providerXform,
        Fixture fixture)
    {
        var providerTransform = _physics.GetPhysicsTransform(provider, providerXform);
        for (var child = 0; child < fixture.Shape.ChildCount; child++)
        {
            if (_manifold.TestOverlap(
                    _supportProbe,
                    0,
                    fixture.Shape,
                    child,
                    sample.Transform,
                    providerTransform,
                    ignoreShapeSkin: true))
            {
                return true;
            }
        }

        return false;
    }

    private bool TryGetSupportFixture(Entity<ZLevelHighGroundComponent> provider, out Fixture fixture)
    {
        fixture = default!;
        if (string.IsNullOrWhiteSpace(provider.Comp.SurfaceFixture) ||
            !_fixturesQuery.TryComp(provider.Owner, out var fixtures) ||
            !fixtures.Fixtures.TryGetValue(provider.Comp.SurfaceFixture, out var found))
        {
            return false;
        }

        fixture = found;
        return true;
    }

    private static void SelectBetterSupport(ref ZLevelSupportResult selected, in ZLevelSupportResult support)
    {
        if (selected.Surface == ZLevelSupportSurface.None ||
            support.AbsoluteHeight > selected.AbsoluteHeight ||
            support.AbsoluteHeight.Equals(selected.AbsoluteHeight) &&
            (support.Surface > selected.Surface ||
             support.Surface == selected.Surface &&
             support.Provider.GetValueOrDefault().CompareTo(selected.Provider.GetValueOrDefault()) < 0))
        {
            selected = support;
        }
    }

    private readonly record struct SupportSample(
        Vector2 Point,
        Box2 Bounds,
        PhysicsTransform Transform);
}

public readonly record struct ZLevelSupportResult(
    EntityUid? Provider,
    ZLevelSupportSurface Surface,
    float AbsoluteHeight,
    Vector2 SamplePoint)
{
    public static readonly ZLevelSupportResult None = new(null, ZLevelSupportSurface.None, 0f, default);
}
