using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Numerics;
using Robust.Shared.Analyzers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Robust.Shared.GameObjects;

public sealed partial class ZLevelSystem
{
    private readonly List<GridMoveTarget> _gridMoveTargets = new();
    private readonly Dictionary<EntityUid, GridMoveTarget> _gridMoveTargetByGrid = new();

    [Pure]
    public bool TryGetGridAbove(EntityUid grid, [NotNullWhen(true)] out EntityUid? above)
    {
        above = null;
        if (!_zGridQuery.TryComp(grid, out var zGrid) ||
            zGrid.GridAbove is not { } linked ||
            !IsValidGridLink(grid, linked, above: true))
        {
            return false;
        }

        above = linked;
        return true;
    }

    [Pure]
    public bool TryGetGridBelow(EntityUid grid, [NotNullWhen(true)] out EntityUid? below)
    {
        below = null;
        if (!_zGridQuery.TryComp(grid, out var zGrid) ||
            zGrid.GridBelow is not { } linked ||
            !IsValidGridLink(grid, linked, above: false))
        {
            return false;
        }

        below = linked;
        return true;
    }

    private bool IsValidGridLink(EntityUid grid, EntityUid linked, bool above)
    {
        if (!_gridQuery.HasComp(grid) ||
            !_gridQuery.HasComp(linked) ||
            !_zGridQuery.TryComp(linked, out var linkedComp) ||
            (above ? linkedComp.GridBelow : linkedComp.GridAbove) != grid ||
            _transform.GetMap(grid) is not { } gridMap ||
            _transform.GetMap(linked) is not { } linkedMap)
        {
            return false;
        }

        return TryGetMapOffset(gridMap, above ? 1 : -1, out var expectedMap) && expectedMap == linkedMap;
    }

    private bool TryResolveLinkedDestinationGrid(
        EntityUid? sourceGrid,
        EntityUid targetMap,
        int offset,
        [NotNullWhen(true)] out EntityUid? targetGrid)
    {
        targetGrid = null;
        if (sourceGrid is not { } current || offset == 0)
            return false;

        var direction = Math.Sign(offset);
        for (var i = 0; i < Math.Abs(offset); i++)
        {
            var found = direction > 0
                ? TryGetGridAbove(current, out var next)
                : TryGetGridBelow(current, out next);
            if (!found || next is not { } linked)
                return false;

            current = linked;
        }

        if (_transform.GetMap(current) != targetMap)
            return false;

        targetGrid = current;
        return true;
    }

    /// <summary>
    /// Explicitly associates corresponding grids on adjacent z maps.
    /// </summary>
    public bool TryLinkGrids(EntityUid lower, EntityUid upper, bool align = true)
    {
        if (lower == upper ||
            !_gridQuery.TryComp(lower, out var lowerGrid) ||
            !_gridQuery.TryComp(upper, out var upperGrid) ||
            _transform.GetMap(lower) is not { } lowerMap ||
            _transform.GetMap(upper) is not { } upperMap ||
            !TryGetMapAbove(lowerMap, out var expectedUpperMap) ||
            expectedUpperMap != upperMap)
        {
            return false;
        }

        var lowerAbove = _zGridQuery.TryComp(lower, out var lowerComp) ? lowerComp.GridAbove : null;
        var upperBelow = _zGridQuery.TryComp(upper, out var upperComp) ? upperComp.GridBelow : null;
        if (lowerAbove == upper && upperBelow == lower)
            return true;

        if (lowerAbove != null || upperBelow != null)
            return false;

        if (align && !TryAlignGridsForLink((lower, lowerGrid), (upper, upperGrid)))
            return false;

        lowerComp = EnsureComp<ZLevelGridComponent>(lower);
        upperComp = EnsureComp<ZLevelGridComponent>(upper);
        RecordGridLink(lower, lowerComp, upper, upperComp);
        return true;
    }

    private void RecordGridLink(
        EntityUid lower,
        ZLevelGridComponent lowerComp,
        EntityUid upper,
        ZLevelGridComponent upperComp)
    {
        var (lowerPosition, lowerRotation) = _transform.GetWorldPositionRotation(lower);
        var (upperPosition, upperRotation) = _transform.GetWorldPositionRotation(upper);

        lowerComp.GridAbove = upper;
        lowerComp.GridAboveOffset = new Angle(-lowerRotation.Theta).RotateVec(upperPosition - lowerPosition);
        lowerComp.GridAboveRotation = upperRotation - lowerRotation;
        upperComp.GridBelow = lower;
        upperComp.GridBelowOffset = new Angle(-upperRotation.Theta).RotateVec(lowerPosition - upperPosition);
        upperComp.GridBelowRotation = lowerRotation - upperRotation;

        DirtyFields(lower, lowerComp, null,
            nameof(ZLevelGridComponent.GridAbove),
            nameof(ZLevelGridComponent.GridAboveOffset),
            nameof(ZLevelGridComponent.GridAboveRotation));
        DirtyFields(upper, upperComp, null,
            nameof(ZLevelGridComponent.GridBelow),
            nameof(ZLevelGridComponent.GridBelowOffset),
            nameof(ZLevelGridComponent.GridBelowRotation));
        RaiseGridLinkChanged(lower, upper);
        RaiseGridLinkChanged(upper, lower);
    }

    private bool TryAlignGridsForLink(Entity<MapGridComponent> lower, Entity<MapGridComponent> upper)
    {
        var lowerArea = GetLinkedGridStackArea(lower);
        var upperArea = GetLinkedGridStackArea(upper);
        return lowerArea >= upperArea
            ? TryAlignGridToAnchor(upper, lower)
            : TryAlignGridToAnchor(lower, upper);
    }

    private bool TryAlignGridToAnchor(Entity<MapGridComponent> moving, Entity<MapGridComponent> anchor)
    {
        if (!_xformQuery.TryComp(moving.Owner, out var movingXform) ||
            movingXform.MapID == MapId.Nullspace ||
            !_xformQuery.TryComp(anchor.Owner, out var anchorXform))
        {
            return false;
        }

        var (movingPosition, movingRotation) = _transform.GetWorldPositionRotation(movingXform);
        var (_, anchorRotation) = _transform.GetWorldPositionRotation(anchorXform);
        var localPosition = Vector2.Transform(movingPosition, _transform.GetInvWorldMatrix(anchorXform));
        var scale = anchor.Comp.TileSize == 0 ? 1f : anchor.Comp.TileSize;
        var snappedLocal = new Vector2(
            MathF.Round(localPosition.X / scale, MidpointRounding.AwayFromZero) * scale,
            MathF.Round(localPosition.Y / scale, MidpointRounding.AwayFromZero) * scale);
        var snappedPosition = Vector2.Transform(snappedLocal, _transform.GetWorldMatrix(anchorXform));
        const double quarterTurn = Math.PI / 2d;
        var relative = movingRotation - anchorRotation;
        var turns = Math.Round(relative.Theta / quarterTurn, MidpointRounding.AwayFromZero);
        var snappedRotation = anchorRotation + new Angle(turns * quarterTurn);

        return TryMoveLinkedGrids(moving.Owner, snappedPosition - movingPosition, snappedRotation - movingRotation);
    }

    private float GetLinkedGridStackArea(Entity<MapGridComponent> root)
    {
        var area = 0f;
        var visited = new HashSet<EntityUid>();
        var pending = new List<EntityUid> { root.Owner };
        for (var i = 0; i < pending.Count; i++)
        {
            var uid = pending[i];
            if (!visited.Add(uid) || !_gridQuery.TryComp(uid, out var grid))
                continue;

            area += Box2.Area(grid.LocalAABB);
            if (!_zGridQuery.TryComp(uid, out var link))
                continue;
            if (link.GridBelow is { } below)
                pending.Add(below);
            if (link.GridAbove is { } above)
                pending.Add(above);
        }

        return area;
    }

    /// <summary>
    /// Tries to unlink a z-level grid to any grids it's attached to.
    /// </summary>
    /// <returns>True if any were unlinked</returns>
    public bool TryUnlinkGrid(EntityUid grid)
    {
        if (!_zGridQuery.TryComp(grid, out var zGrid))
            return false;

        var changed = false;
        if (zGrid.GridAbove is { } above)
        {
            zGrid.GridAbove = null;
            changed = true;
            if (_zGridQuery.TryComp(above, out var aboveGrid) && aboveGrid.GridBelow == grid)
            {
                aboveGrid.GridBelow = null;
                DirtyField(above, aboveGrid, nameof(ZLevelGridComponent.GridBelow));
                RaiseGridLinkChanged(above, grid);
            }
            RaiseGridLinkChanged(grid, above);
        }

        if (zGrid.GridBelow is { } below)
        {
            zGrid.GridBelow = null;
            changed = true;
            if (_zGridQuery.TryComp(below, out var belowGrid) && belowGrid.GridAbove == grid)
            {
                belowGrid.GridAbove = null;
                DirtyField(below, belowGrid, nameof(ZLevelGridComponent.GridAbove));
                RaiseGridLinkChanged(below, grid);
            }
            RaiseGridLinkChanged(grid, below);
        }

        if (changed)
            DirtyFields(grid, zGrid, null, nameof(ZLevelGridComponent.GridAbove), nameof(ZLevelGridComponent.GridBelow));
        return changed;
    }

    [SubscribeLocalEvent]
    private void OnZGridShutdown(Entity<ZLevelGridComponent> entity, ref ComponentShutdown args)
    {
        if (entity.Comp.GridAbove is { } above &&
            _zGridQuery.TryComp(above, out var aboveComp) &&
            aboveComp.GridBelow == entity.Owner)
        {
            aboveComp.GridBelow = null;
            DirtyField(above, aboveComp, nameof(ZLevelGridComponent.GridBelow));
            RaiseGridLinkChanged(above, entity.Owner);
        }

        if (entity.Comp.GridBelow is { } below &&
            _zGridQuery.TryComp(below, out var belowComp) &&
            belowComp.GridAbove == entity.Owner)
        {
            belowComp.GridAbove = null;
            DirtyField(below, belowComp, nameof(ZLevelGridComponent.GridAbove));
            RaiseGridLinkChanged(below, entity.Owner);
        }
    }

    private void RaiseGridLinkChanged(EntityUid grid, EntityUid otherGrid)
    {
        var ev = new ZLevelGridLinkChangedEvent(grid, otherGrid);
        RaiseLocalEvent(grid, ev);
    }

    /// <summary>
    /// Moves a complete linked stack while preserving every relative pose.
    /// </summary>
    public bool TryMoveLinkedGrids(EntityUid grid, Vector2 translation, Angle rotation = default)
    {
        if (!TryBuildGridMoveTargets(grid, translation, rotation))
            return false;

        foreach (var target in _gridMoveTargets)
            _transform.SetWorldPositionRotation(target.Grid, target.Position, target.Rotation);
        return true;
    }

    private bool TryBuildGridMoveTargets(EntityUid root, Vector2 translation, Angle rotation)
    {
        _gridMoveTargets.Clear();
        _gridMoveTargetByGrid.Clear();
        if (!_gridQuery.HasComp(root) || !_xformQuery.TryComp(root, out var rootXform) || rootXform.MapID == MapId.Nullspace)
            return false;

        var (position, worldRotation) = _transform.GetWorldPositionRotation(rootXform);
        if (!TryAddGridMoveTarget(root, position + translation, worldRotation + rotation))
            return false;

        for (var i = 0; i < _gridMoveTargets.Count; i++)
        {
            var target = _gridMoveTargets[i];
            if (!_zGridQuery.TryComp(target.Grid, out var zGrid))
                continue;

            if (zGrid.GridAbove is { } above &&
                !TryAddLinkedGridMoveTarget(target, above, zGrid.GridAboveOffset, zGrid.GridAboveRotation, below: false))
                return false;
            if (zGrid.GridBelow is { } below &&
                !TryAddLinkedGridMoveTarget(target, below, zGrid.GridBelowOffset, zGrid.GridBelowRotation, below: true))
                return false;
        }

        return true;
    }

    private bool TryAddLinkedGridMoveTarget(
        GridMoveTarget source,
        EntityUid linked,
        Vector2 localOffset,
        Angle localRotation,
        bool below)
    {
        if (!_zGridQuery.TryComp(linked, out var linkedComp) ||
            (below ? linkedComp.GridAbove : linkedComp.GridBelow) != source.Grid)
            return false;

        return TryAddGridMoveTarget(
            linked,
            source.Position + source.Rotation.RotateVec(localOffset),
            source.Rotation + localRotation);
    }

    private bool TryAddGridMoveTarget(EntityUid grid, Vector2 position, Angle rotation)
    {
        if (!_gridQuery.HasComp(grid) || !_xformQuery.TryComp(grid, out var xform) || xform.MapID == MapId.Nullspace)
            return false;

        if (_gridMoveTargetByGrid.TryGetValue(grid, out var existing))
            return existing.Position.EqualsApprox(position) && MathHelper.CloseTo(existing.Rotation.Theta, rotation.Theta);

        var target = new GridMoveTarget(grid, position, rotation);
        _gridMoveTargetByGrid.Add(grid, target);
        _gridMoveTargets.Add(target);
        return true;
    }

    private void PruneInvalidGridLinks(EntityUid network, IReadOnlyList<EntityUid> maps)
    {
        var depths = new Dictionary<EntityUid, int>(maps.Count);
        for (var i = 0; i < maps.Count; i++)
            depths[maps[i]] = i;

        var unlink = new List<EntityUid>();
        var query = AllEntityQuery<ZLevelGridComponent>();
        while (query.MoveNext(out var lower, out var link))
        {
            if (link.GridAbove is not { } upper ||
                _transform.GetMap(lower) is not { } lowerMap ||
                _transform.GetMap(upper) is not { } upperMap)
                continue;

            // SetNetworkLevels is network-local. Do not disturb an independent z stack merely because its maps are
            // absent from this network's replacement list.
            if (!_zMapQuery.TryComp(lowerMap, out var lowerZMap) || lowerZMap.Network != network)
                continue;

            if (!depths.TryGetValue(lowerMap, out var lowerDepth) ||
                !depths.TryGetValue(upperMap, out var upperDepth) ||
                upperDepth != lowerDepth + 1)
            {
                unlink.Add(lower);
            }
        }

        foreach (var grid in unlink)
            TryUnlinkGrid(grid);
    }

    private void UnlinkNetworkGrids(EntityUid network)
    {
        PruneInvalidGridLinks(network, Array.Empty<EntityUid>());
    }

    private void UnlinkGridsOnMap(EntityUid map)
    {
        var unlink = new List<EntityUid>();
        var query = AllEntityQuery<ZLevelGridComponent, TransformComponent>();
        while (query.MoveNext(out var grid, out _, out var xform))
        {
            if (xform.MapUid == map)
                unlink.Add(grid);
        }

        foreach (var grid in unlink)
            TryUnlinkGrid(grid);
    }

    private void CaptureGridLinks()
    {
        var query = AllEntityQuery<ZLevelGridComponent>();
        while (query.MoveNext(out var lower, out var link))
        {
            if (link.GridAbove is not { } upper ||
                !IsValidGridLink(lower, upper, above: true) ||
                _transform.GetMap(lower) is not { } lowerMap ||
                !TryGetMapNetwork(lowerMap, out var network) ||
                network is not { } networkEntity)
                continue;

            networkEntity.Comp.GridLinks.Add(new ZLevelGridLink(lower, upper));
        }
    }

    private void InitializeGridMap(EntityUid grid)
    {
        if (_xformQuery.TryComp(grid, out var xform))
            _transform.InitializeMapUid(grid, xform);
    }

    private bool CanRestoreGridLinks(Entity<ZLevelMapNetworkComponent> network)
    {
        var lowerLinks = new HashSet<EntityUid>();
        var upperLinks = new HashSet<EntityUid>();
        foreach (var link in network.Comp.GridLinks)
        {
            if (link.Lower == link.Upper ||
                !lowerLinks.Add(link.Lower) ||
                !upperLinks.Add(link.Upper) ||
                !_gridQuery.HasComp(link.Lower) ||
                !_gridQuery.HasComp(link.Upper) ||
                _transform.GetMap(link.Lower) is not { } lowerMap ||
                _transform.GetMap(link.Upper) is not { } upperMap ||
                !TryGetMapAbove(lowerMap, out var expectedUpper) ||
                expectedUpper != upperMap)
            {
                return false;
            }
        }

        return true;
    }

    private readonly record struct GridMoveTarget(EntityUid Grid, Vector2 Position, Angle Rotation);
}

/// <summary>
/// Raised on either grid when an explicit z-level association is created or removed.
/// </summary>
public readonly record struct ZLevelGridLinkChangedEvent(EntityUid Grid, EntityUid OtherGrid);
