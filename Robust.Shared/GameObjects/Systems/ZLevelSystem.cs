using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using Robust.Shared.EntitySerialization;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Map.Events;
using Robust.Shared.Maths;

namespace Robust.Shared.GameObjects;

/// <summary>
/// Shared queries and mutation helpers for ordered z-map networks.
/// </summary>
public sealed partial class ZLevelSystem : EntitySystem
{
    private const string ZLevelMapNetworkPrototype = "ZLevelMapNetwork";

    [Dependency] private SharedMapSystem _mapSystem = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedPvsOverrideSystem _pvsOverride = default!;
    [Dependency] private EntityQuery<MapComponent> _mapQuery = default!;
    [Dependency] private EntityQuery<MapGridComponent> _gridQuery = default!;
    [Dependency] private EntityQuery<TransformComponent> _xformQuery = default!;
    [Dependency] private EntityQuery<ZLevelGridComponent> _zGridQuery = default!;
    [Dependency] private EntityQuery<ZLevelMapComponent> _zMapQuery = default!;
    [Dependency] private EntityQuery<ZLevelMapNetworkComponent> _networkQuery = default!;

    private List<Entity<MapGridComponent>> _renderOccluderGrids = new();

    public Entity<ZLevelMapNetworkComponent> CreateMapNetwork()
    {
        var uid = Spawn(ZLevelMapNetworkPrototype);
        return (uid, _networkQuery.GetComponent(uid));
    }

    /// <summary>
    /// Creates a network from maps ordered from lowest to highest.
    /// </summary>
    public bool TryCreateMapNetwork(
        IReadOnlyList<EntityUid> maps,
        [NotNullWhen(true)] out Entity<ZLevelMapNetworkComponent>? network)
    {
        network = null;
        if (maps.Count == 0 || HasDuplicateMaps(maps))
            return false;

        var created = CreateMapNetwork();
        var depths = new Dictionary<EntityUid, int>(maps.Count);
        for (var i = 0; i < maps.Count; i++)
            depths.Add(maps[i], i);

        if (!TryAddMaps(created, depths))
        {
            PredictedQueueDel(created);
            return false;
        }

        network = created;
        return true;
    }

    /// <summary>
    /// Adds maps to an empty network. The supplied depths establish their ordering.
    /// </summary>
    public bool TryAddMaps(Entity<ZLevelMapNetworkComponent> network, IReadOnlyDictionary<EntityUid, int> maps)
    {
        if (maps.Count == 0)
            return false;

        foreach (var map in maps.Keys)
        {
            if (!_mapQuery.HasComp(map) || _zMapQuery.HasComp(map))
                return false;
        }

        var sorted = new List<EntityUid>(maps.Count);
        foreach (var map in maps.Keys)
        {
            var depth = maps[map];
            var index = 0;
            while (index < sorted.Count && maps[sorted[index]] < depth)
                index++;
            if (index < sorted.Count && maps[sorted[index]] == depth)
                return false;
            sorted.Insert(index, map);
        }
        if (network.Comp.SortedZLevelsInternal.Count == 0)
        {
            SetNetworkLevels(network, sorted);
            return true;
        }

        return TryInsertMaps(network, network.Comp.SortedZLevelsInternal.Count, sorted);
    }

    public bool TryInsertMap(Entity<ZLevelMapNetworkComponent> network, int index, EntityUid map)
        => TryInsertMaps(network, index, [map]);

    private bool TryInsertMaps(Entity<ZLevelMapNetworkComponent> network, int index, IReadOnlyList<EntityUid> maps)
    {
        if (maps.Count == 0 ||
            index < 0 ||
            index > network.Comp.SortedZLevelsInternal.Count ||
            HasDuplicateMaps(maps) ||
            !HasLinearDepths(network))
        {
            return false;
        }

        foreach (var map in maps)
        {
            if (!_mapQuery.HasComp(map) || _zMapQuery.HasComp(map))
                return false;
        }

        var levels = new List<EntityUid>(network.Comp.SortedZLevelsInternal);
        levels.InsertRange(index, maps);
        SetNetworkLevels(network, levels);
        return true;
    }

    [Pure]
    public bool TryGetMapNetwork(EntityUid map, [NotNullWhen(true)] out Entity<ZLevelMapNetworkComponent>? network)
    {
        network = null;
        if (!_zMapQuery.TryComp(map, out var zMap) || !_networkQuery.TryComp(zMap.Network, out var comp))
            return false;

        network = (zMap.Network, comp);
        return true;
    }

    [Pure]
    public bool TryGetMapData(
        EntityUid map,
        [NotNullWhen(true)] out ZLevelMapComponent? zMap,
        [NotNullWhen(true)] out ZLevelMapNetworkComponent? network)
    {
        network = null;
        return _zMapQuery.TryComp(map, out zMap) && _networkQuery.TryComp(zMap.Network, out network);
    }

    [Pure]
    public bool TryGetMapDepthOffset(EntityUid fromMap, EntityUid toMap, out int offset)
    {
        offset = 0;
        if (!_zMapQuery.TryComp(fromMap, out var from) ||
            !_zMapQuery.TryComp(toMap, out var to) ||
            from.Network != to.Network)
        {
            return false;
        }

        offset = to.Depth - from.Depth;
        return true;
    }

    public void CollectMapNetworks(List<Entity<ZLevelMapNetworkComponent>> networks)
    {
        networks.Clear();
        var query = AllEntityQuery<ZLevelMapNetworkComponent>();
        while (query.MoveNext(out var uid, out var comp))
            networks.Add((uid, comp));
    }

    [Pure]
    public bool TryGetMapAtDepth(EntityUid networkUid, int depth, [NotNullWhen(true)] out EntityUid? map)
    {
        map = null;
        if (!_networkQuery.TryComp(networkUid, out var network) ||
            depth < 0 ||
            depth >= network.SortedZLevelsInternal.Count)
        {
            return false;
        }

        map = network.SortedZLevelsInternal[depth];
        return true;
    }

    [Pure]
    public bool TryGetMapOffset(EntityUid map, int offset, [NotNullWhen(true)] out EntityUid? other)
    {
        other = null;
        if (!_zMapQuery.TryComp(map, out var zMap) ||
            !TryGetMapAtDepth(zMap.Network, zMap.Depth + offset, out other))
        {
            return false;
        }

        return true;
    }

    [Pure]
    public bool TryGetMapAbove(EntityUid map, [NotNullWhen(true)] out EntityUid? above)
        => TryGetMapOffset(map, 1, out above);

    [Pure]
    public bool TryGetMapBelow(EntityUid map, [NotNullWhen(true)] out EntityUid? below)
        => TryGetMapOffset(map, -1, out below);

    /// <summary>
    /// Moves an entity between linked maps while preserving its position and rotation.
    /// </summary>
    public bool TryMoveEntityToMapOffset(EntityUid entity, int offset, [NotNullWhen(true)] out EntityUid? targetMap)
    {
        targetMap = null;
        var xform = Transform(entity);
        if (offset == 0 ||
            xform.MapUid is not { } currentMap ||
            !TryGetMapOffset(currentMap, offset, out targetMap) ||
            targetMap is not { } target ||
            !_mapQuery.TryComp(target, out var targetMapComp))
        {
            targetMap = null;
            return false;
        }

        var (position, rotation) = _transform.GetWorldPositionRotation(xform);
        if (TryResolveLinkedDestinationGrid(xform.GridUid, target, offset, out var targetGrid))
        {
            var targetCoordinates = _transform.ToCoordinates(
                targetGrid.Value,
                new MapCoordinates(position, targetMapComp.MapId));
            _transform.SetCoordinates(entity, xform, targetCoordinates, rotation - _transform.GetWorldRotation(targetGrid.Value));
        }
        else
        {
            // Do not attach grid children to an unrelated overlapping grid.
            if (xform.GridUid != null)
                _transform.SetCoordinates(entity, xform, new EntityCoordinates(target, position), rotation);
            else
                _transform.SetMapCoordinates((entity, xform), new MapCoordinates(position, targetMapComp.MapId), rotation);
        }

        return true;
    }

    public bool TryMoveEntityToMapOffset(EntityUid entity, int offset)
        => TryMoveEntityToMapOffset(entity, offset, out _);

    public void CollectMapOffsets(EntityUid map, int below, int above, List<EntityUid> maps)
    {
        maps.Clear();
        if (!TryGetMapData(map, out var zMap, out var network))
            return;

        for (var offset = -Math.Max(0, below); offset <= Math.Max(0, above); offset++)
        {
            if (offset == 0)
                continue;

            var depth = zMap.Depth + offset;
            if (depth >= 0 && depth < network.SortedZLevels.Count)
                maps.Add(network.SortedZLevels[depth]);
        }
    }

    [Pure]
    public bool TryGetMapDepth(EntityUid map, [NotNullWhen(true)] out int? depth)
    {
        if (_zMapQuery.TryComp(map, out var zMap))
        {
            depth = zMap.Depth;
            return true;
        }

        depth = null;
        return false;
    }

    public void CollectRenderableMaps(
        EntityUid mapUid,
        int below,
        int above,
        List<MapId> belowMaps,
        List<MapId> aboveMaps)
    {
        belowMaps.Clear();
        aboveMaps.Clear();
        if (!_zMapQuery.HasComp(mapUid))
            return;

        CollectRenderableMapDirection(mapUid, -1, below, belowMaps);
        CollectRenderableMapDirection(mapUid, 1, above, aboveMaps);
    }

    private void CollectRenderableMapDirection(EntityUid mapUid, int direction, int count, List<MapId> maps)
    {
        for (var current = mapUid; maps.Count < Math.Max(0, count);)
        {
            if (!TryGetMapOffset(current, direction, out var next) ||
                !_mapQuery.TryComp(next.Value, out var map))
            {
                return;
            }

            maps.Add(map.MapId);
            current = next.Value;
        }
    }

    /// <summary>
    /// Checks whether holes allow the next lower map to be seen.
    /// </summary>
    public bool NeedsLowerLevelRendered(MapId mapId, Box2 worldAABB, Box2Rotated worldBounds)
    {
        _renderOccluderGrids.Clear();
        _mapSystem.FindGridsIntersecting(mapId, worldAABB.Enlarged(1f), ref _renderOccluderGrids, approx: true);

        foreach (var grid in _renderOccluderGrids)
        {
            var matrix = _transform.GetWorldMatrix(grid.Owner);
            var gridAabb = matrix.TransformBox(grid.Comp.LocalAABB).Enlarged(grid.Comp.TileSize * 2f);
            if (gridAabb.Contains(worldAABB))
                return GridViewportContainsEmptyTile(grid, worldBounds);
        }

        return true;
    }

    [Pure]
    private bool GridViewportContainsEmptyTile(Entity<MapGridComponent> grid, Box2Rotated worldBounds)
    {
        var localAabb = _transform.GetInvWorldMatrix(grid.Owner).TransformBox(worldBounds).Enlarged(grid.Comp.TileSize);
        var tileSize = grid.Comp.TileSize;
        var min = new Vector2i(
            (int) MathF.Floor(localAabb.Left / tileSize) - 1,
            (int) MathF.Floor(localAabb.Bottom / tileSize) - 1);
        var max = new Vector2i(
            (int) MathF.Ceiling(localAabb.Right / tileSize) + 1,
            (int) MathF.Ceiling(localAabb.Top / tileSize) + 1);
        var chunkSize = grid.Comp.ChunkSize;
        var minChunk = _mapSystem.GridTileToChunkIndices(grid.Comp, min);
        var maxChunk = _mapSystem.GridTileToChunkIndices(grid.Comp, max);

        for (var chunkX = minChunk.X; chunkX <= maxChunk.X; chunkX++)
        {
            for (var chunkY = minChunk.Y; chunkY <= maxChunk.Y; chunkY++)
            {
                var chunkIndex = new Vector2i(chunkX, chunkY);
                if (!grid.Comp.Chunks.TryGetValue(chunkIndex, out var chunk))
                    return true;

                var origin = chunkIndex * chunkSize;
                var relativeMin = min - origin;
                var relativeMax = max - origin;
                var localMin = new Vector2i(Math.Max(0, relativeMin.X), Math.Max(0, relativeMin.Y));
                var localMax = new Vector2i(
                    Math.Min(chunkSize - 1, relativeMax.X),
                    Math.Min(chunkSize - 1, relativeMax.Y));
                for (var x = localMin.X; x <= localMax.X; x++)
                {
                    for (var y = localMin.Y; y <= localMax.Y; y++)
                    {
                        var tile = chunk.GetTile((ushort) x, (ushort) y);
                        if (tile.IsEmpty)
                            return true;
                    }
                }
            }
        }

        return false;
    }

    [Pure]
    public bool HasLinearDepths(Entity<ZLevelMapNetworkComponent> network)
    {
        for (var i = 0; i < network.Comp.SortedZLevels.Count; i++)
        {
            var map = network.Comp.SortedZLevels[i];
            if (!_zMapQuery.TryComp(map, out var zMap) || zMap.Network != network.Owner || zMap.Depth != i)
                return false;
        }

        return true;
    }

    public bool TryRemoveMapFromNetwork(EntityUid map)
    {
        if (!TryGetMapNetwork(map, out var network) || network is not { } entity)
            return false;

        var levels = new List<EntityUid>(entity.Comp.SortedZLevels);
        if (!levels.Remove(map))
            return false;

        UnlinkGridsOnMap(map);
        RemCompDeferred<ZLevelMapComponent>(map);
        if (levels.Count == 0)
            PredictedQueueDel(entity.Owner);
        else
            SetNetworkLevels(entity, levels);

        return true;
    }

    [SubscribeLocalEvent]
    private void OnZMapShutdown(Entity<ZLevelMapComponent> entity, ref ComponentShutdown args)
    {
        UnlinkGridsOnMap(entity.Owner);
        if (!_networkQuery.TryComp(entity.Comp.Network, out var networkComp))
            return;

        Entity<ZLevelMapNetworkComponent> network = (entity.Comp.Network, networkComp);
        var levels = new List<EntityUid>(network.Comp.SortedZLevels);
        if (!levels.Remove(entity.Owner))
            return;

        if (levels.Count == 0)
        {
            if (!TerminatingOrDeleted(network.Owner))
                PredictedQueueDel(network.Owner);
        }
        else if (!TerminatingOrDeleted(network.Owner))
        {
            SetNetworkLevels(network, levels);
        }
    }

    [SubscribeLocalEvent]
    private void OnNetworkStartup(Entity<ZLevelMapNetworkComponent> entity, ref ComponentStartup args)
    {
        // Nullspace network entities need a PVS override.
        _pvsOverride.AddGlobalOverride(entity);
        if (entity.Comp.Maps.Count == 0)
            return;

        var maps = entity.Comp.Maps;
        var invalid = HasDuplicateMaps(maps);
        for (var i = 0; i < maps.Count && !invalid; i++)
            invalid = !_mapQuery.HasComp(maps[i]) || _zMapQuery.HasComp(maps[i]);
        if (invalid)
        {
            Log.Error($"Unable to restore z-level network {ToPrettyString(entity)}: its saved map list is invalid.");
            return;
        }

        SetNetworkLevels(entity, entity.Comp.Maps);
        if (!CanRestoreGridLinks(entity))
        {
            Log.Error($"Unable to restore z-level grid links for {ToPrettyString(entity)}: the saved links are invalid.");
            return;
        }

        foreach (var link in entity.Comp.GridLinks)
        {
            InitializeGridMap(link.Lower);
            InitializeGridMap(link.Upper);
            if (!TryLinkGrids(link.Lower, link.Upper, align: false))
                Log.Error($"Unable to restore z-level grid link {ToPrettyString(link.Lower)} -> {ToPrettyString(link.Upper)}.");
        }
    }

    [SubscribeLocalEvent]
    private void OnNetworkShutdown(Entity<ZLevelMapNetworkComponent> entity, ref ComponentShutdown args)
    {
        _pvsOverride.RemoveGlobalOverride(entity);
        UnlinkNetworkGrids(entity.Owner);
        foreach (var map in entity.Comp.SortedZLevels)
        {
            if (_zMapQuery.TryComp(map, out var zMap) && zMap.Network == entity.Owner)
                RemCompDeferred<ZLevelMapComponent>(map);
        }

        var ev = new ZLevelNetworkShutdownEvent();
        RaiseLocalEvent(entity.Owner, ref ev);
    }

    private static bool HasDuplicateMaps(IReadOnlyList<EntityUid> maps)
    {
        for (var i = 0; i < maps.Count; i++)
        {
            for (var j = i + 1; j < maps.Count; j++)
            {
                if (maps[i] == maps[j])
                    return true;
            }
        }

        return false;
    }

    [SubscribeLocalEvent]
    private void OnBeforeSerialization(BeforeSerializationEvent args)
    {
        if (args.Category != FileCategory.Save)
            return;

        var query = AllEntityQuery<ZLevelMapNetworkComponent>();
        while (query.MoveNext(out _, out var network))
        {
            network.Maps.Clear();
            network.Maps.AddRange(network.SortedZLevels);
            network.GridLinks.Clear();
        }

        CaptureGridLinks();
    }

    private void SetNetworkLevels(Entity<ZLevelMapNetworkComponent> network, IReadOnlyList<EntityUid> maps)
    {
        PruneInvalidGridLinks(network.Owner, maps);
        network.Comp.SortedZLevelsInternal.Clear();
        for (var depth = 0; depth < maps.Count; depth++)
        {
            var map = maps[depth];
            network.Comp.SortedZLevelsInternal.Add(map);
            var zMap = EnsureComp<ZLevelMapComponent>(map);
            zMap.Network = network.Owner;
            zMap.Depth = depth;
            DirtyFields(map, zMap, null, nameof(ZLevelMapComponent.Network), nameof(ZLevelMapComponent.Depth));
        }

        DirtyField(network.Owner, network.Comp, nameof(ZLevelMapNetworkComponent.SortedZLevelsInternal));
    }
}
