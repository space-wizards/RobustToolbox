using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Map.Enumerators;
using Robust.Shared.Map.Events;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;

public abstract partial class SharedMapSystem
{
    #region Chunk helpers

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2i GetChunkIndices(Vector2 tile, int chunkSize)
    {
        return new Vector2i ((int) Math.Floor(tile.X / chunkSize), (int) Math.Floor(tile.Y / chunkSize));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2i GetChunkIndices(Vector2 tile, byte chunkSize)
    {
        return new Vector2i ((int) Math.Floor(tile.X / chunkSize), (int) Math.Floor(tile.Y / chunkSize));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2i GetChunkIndices(Vector2i tile, int chunkSize)
    {
        return new Vector2i ((int) Math.Floor(tile.X / (float) chunkSize), (int) Math.Floor(tile.Y / (float) chunkSize));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2i GetChunkIndices(Vector2i tile, byte chunkSize)
    {
        return new Vector2i ((int) Math.Floor(tile.X / (float) chunkSize), (int) Math.Floor(tile.Y / (float) chunkSize));
    }

    /// <summary>
    /// Returns the tile offset to a chunk origin based on the provided size.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2i GetChunkRelative(Vector2 tile, int chunkSize)
    {
        var x = MathHelper.Mod((int) Math.Floor(tile.X), chunkSize);
        var y = MathHelper.Mod((int) Math.Floor(tile.Y), chunkSize);
        return new Vector2i(x, y);
    }

    /// <summary>
    /// Returns the tile offset to a chunk origin based on the provided size.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2i GetChunkRelative(Vector2 tile, byte chunkSize)
    {
        var x = MathHelper.Mod((int) Math.Floor(tile.X), chunkSize);
        var y = MathHelper.Mod((int) Math.Floor(tile.Y), chunkSize);
        return new Vector2i(x, y);
    }

    /// <summary>
    /// Returns the tile offset to a chunk origin based on the provided size.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2i GetChunkRelative(Vector2i tile, int chunkSize)
    {
        var x = MathHelper.Mod(tile.X, chunkSize);
        var y = MathHelper.Mod(tile.Y, chunkSize);
        return new Vector2i(x, y);
    }

    /// <summary>
    /// Returns the tile offset to a chunk origin based on the provided size.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2i GetChunkRelative(Vector2i tile, byte chunkSize)
    {
        var x = MathHelper.Mod(tile.X, chunkSize);
        var y = MathHelper.Mod(tile.Y, chunkSize);
        return new Vector2i(x, y);
    }

    #endregion

    public static Vector2i GetDirection(Vector2i position, Direction dir, int dist = 1)
    {
        switch (dir)
        {
            case Direction.East:
                return position + new Vector2i(dist, 0);
            case Direction.SouthEast:
                return position + new Vector2i(dist, -dist);
            case Direction.South:
                return position + new Vector2i(0, -dist);
            case Direction.SouthWest:
                return position + new Vector2i(-dist, -dist);
            case Direction.West:
                return position + new Vector2i(-dist, 0);
            case Direction.NorthWest:
                return position + new Vector2i(-dist, dist);
            case Direction.North:
                return position + new Vector2i(0, dist);
            case Direction.NorthEast:
                return position + new Vector2i(dist, dist);
            default:
                throw new NotImplementedException();
        }
    }

    private void InitializeGrid()
    {
        SubscribeLocalEvent<MapGridComponent, ComponentGetState>(OnGridGetState);
        SubscribeLocalEvent<MapGridComponent, ComponentHandleState>(OnGridHandleState);
        SubscribeLocalEvent<MapGridComponent, ComponentAdd>(OnGridAdd);
        SubscribeLocalEvent<MapGridComponent, ComponentInit>(OnGridInit);
        SubscribeLocalEvent<MapGridComponent, ComponentStartup>(OnGridStartup);
        SubscribeLocalEvent<MapGridComponent, ComponentShutdown>(OnGridRemove);
        SubscribeLocalEvent<MapGridComponent, MoveEvent>(OnGridMove);
    }

    /// <summary>
    /// <see cref="GetGridPosition(Robust.Shared.GameObjects.Entity{Robust.Shared.Physics.Components.PhysicsComponent?},System.Numerics.Vector2,Robust.Shared.Maths.Angle)"/>
    /// </summary>
    public Vector2 GetGridPosition(Entity<PhysicsComponent?> grid, Vector2 worldPos, Angle worldRot)
    {
        if (!Resolve(grid.Owner, ref grid.Comp))
            return Vector2.Zero;

        return worldPos + worldRot.RotateVec(grid.Comp.LocalCenter);
    }

    /// <summary>
    /// Gets the mapgrid's position considering its local physics center.
    /// </summary>
    public Vector2 GetGridPosition(Entity<PhysicsComponent?, TransformComponent?> grid)
    {
        if (!Resolve(grid.Owner, ref grid.Comp1, ref grid.Comp2))
            return Vector2.Zero;

        var (worldPos, worldRot) = _transform.GetWorldPositionRotation(grid.Comp2);

        return GetGridPosition((grid.Owner, grid.Comp1), worldPos, worldRot);
    }

    private void OnGridBoundsChange(EntityUid uid, MapGridComponent component)
    {
        // Just MapLoader things.
        if (component.MapProxy == DynamicTree.Proxy.Free) return;

        var xform = EntityManager.GetComponent<TransformComponent>(uid);
        var aabb = GetWorldAABB(uid, component, xform);

        if (TryComp<GridTreeComponent>(xform.MapUid, out var gridTree))
        {
            gridTree.Tree.MoveProxy(component.MapProxy, in aabb);
        }

        if (TryComp<MovedGridsComponent>(xform.MapUid, out var movedGrids))
        {
            movedGrids.MovedGrids.Add(uid);
        }
    }

    private void OnGridMove(EntityUid uid, MapGridComponent component, ref MoveEvent args)
    {
        if (args.ParentChanged)
        {
            OnParentChange(uid, component, ref args);
            return;
        }

        // Just maploader / test things
        if (component.MapProxy == DynamicTree.Proxy.Free) return;

        var xform = args.Component;
        var aabb = GetWorldAABB(uid, component, xform);

        if (TryComp<GridTreeComponent>(xform.MapUid, out var gridTree))
        {
            gridTree.Tree.MoveProxy(component.MapProxy, in aabb);
        }

        if (TryComp<MovedGridsComponent>(xform.MapUid, out var movedGrids))
        {
            movedGrids.MovedGrids.Add(uid);
        }
    }

    private void OnParentChange(EntityUid uid, MapGridComponent component, ref MoveEvent args)
    {
        UpdatePvsChunks(args.Entity);

        var (_, xform, meta) = args.Entity;

        // oh boy
        // Want gridinit to handle this hence specialcase those situations.
        // oh boy oh boy, its even worse now.
        // transform now raises parent change events on startup, because container code is a POS.
        if (meta.EntityLifeStage < EntityLifeStage.Initialized || args.Component.LifeStage == ComponentLifeStage.Starting)
            return;

        // yipeee grids are being spontaneously moved to nullspace.
        Log.Info($"Grid {ToPrettyString(uid, meta)} changed parent. Old parent: {ToPrettyString(args.OldPosition.EntityId)}. New parent: {ToPrettyString(xform.ParentUid)}");
        if (xform.MapUid == null && meta.EntityLifeStage < EntityLifeStage.Terminating && _netManager.IsServer)
            Log.Error($"Grid {ToPrettyString(uid, meta)} was moved to nullspace! AAAAAAAAAAAAAAAAAAAAAAAAA! {Environment.StackTrace}");

        DebugTools.Assert(!_mapQuery.HasComponent(uid));

        if (xform.ParentUid != xform.MapUid && meta.EntityLifeStage < EntityLifeStage.Terminating  && _netManager.IsServer)
        {
            Log.Error($"Grid {ToPrettyString(uid, meta)} is not parented to {ToPrettyString(xform._parent)} which is not a map.  y'all need jesus. {Environment.StackTrace}");
            return;
        }

        // Make sure we cleanup old map for moved grid stuff.
        var oldMap = _transform.ToMapCoordinates(args.OldPosition);
        var oldMapUid = GetMapOrInvalid(oldMap.MapId);
        if (component.MapProxy != DynamicTree.Proxy.Free && TryComp<MovedGridsComponent>(oldMapUid, out var oldMovedGrids))
        {
            oldMovedGrids.MovedGrids.Remove(uid);
            RemoveGrid(uid, component, oldMapUid);
        }

        DebugTools.Assert(component.MapProxy == DynamicTree.Proxy.Free);
        if (TryComp<MovedGridsComponent>(xform.MapUid, out var newMovedGrids))
        {
            newMovedGrids.MovedGrids.Add(uid);
            AddGrid(uid, component);
        }
    }

    protected virtual void UpdatePvsChunks(Entity<TransformComponent, MetaDataComponent> grid)
    {
    }

    private void OnGridHandleState(EntityUid uid, MapGridComponent component, ref ComponentHandleState args)
    {
        switch (args.Current)
        {
            case MapGridComponentDeltaState delta:
            {
                DebugTools.Assert(component.ChunkSize == delta.ChunkSize || component.Chunks.Count == 0,
                    "Can't modify chunk size of an existing grid.");

                component.ChunkSize = delta.ChunkSize;
                if (delta.ChunkData == null)
                    return;

                foreach (var (index, chunkData) in delta.ChunkData)
                {
                    ApplyChunkData(uid, component, index, chunkData);
                }

                component.LastTileModifiedTick = delta.LastTileModifiedTick;
                break;
            }
            case MapGridComponentState state:
            {
                DebugTools.Assert(component.ChunkSize == state.ChunkSize || component.Chunks.Count == 0,
                    "Can't modify chunk size of an existing grid.");

                component.LastTileModifiedTick = state.LastTileModifiedTick;
                component.ChunkSize = state.ChunkSize;

                foreach (var index in component.Chunks.Keys)
                {
                    if (!state.FullGridData.ContainsKey(index))
                        ApplyChunkData(uid, component, index, ChunkDatum.Empty);
                }

                foreach (var (index, data) in state.FullGridData)
                {
                    DebugTools.Assert(!data.IsDeleted());
                    ApplyChunkData(uid, component, index, data);
                }

                break;
            }
            default:
                return;
        }

        RegenerateAabb(component);
        OnGridBoundsChange(uid, component);

#if DEBUG
        foreach (var chunk in component.Chunks.Values)
        {
            chunk.ValidateChunk();
            DebugTools.Assert(chunk.FilledTiles > 0);
        }
#endif
    }

    private void ApplyChunkData(
        EntityUid uid,
        MapGridComponent component,
        Vector2i index,
        ChunkDatum data)
    {
        var counter = 0;
        var gridEnt = new Entity<MapGridComponent>(uid, component);

        if (data.IsDeleted())
        {
            if (!component.Chunks.Remove(index, out var deletedChunk))
                return;

            // Deleted chunks still need to raise tile-changed events.
            deletedChunk.SuppressCollisionRegeneration = true;
            for (ushort x = 0; x < component.ChunkSize; x++)
            {
                for (ushort y = 0; y < component.ChunkSize; y++)
                {
                    if (!deletedChunk.TrySetTile(x, y, Tile.Empty, out var oldTile, out _))
                        continue;

                    var gridIndices = deletedChunk.ChunkTileToGridTile((x, y));
                    var newTileRef = new TileRef(uid, gridIndices, Tile.Empty);
                    _mapInternal.RaiseOnTileChanged(gridEnt, newTileRef, oldTile, index);
                }
            }

            deletedChunk.CachedBounds = Box2i.Empty;
            deletedChunk.SuppressCollisionRegeneration = false;
            return;
        }

        var chunk = GetOrAddChunk(uid, component, index);
        chunk.Fixtures.Clear();
        chunk.Fixtures.UnionWith(data.Fixtures);

        chunk.SuppressCollisionRegeneration = true;
        DebugTools.Assert(data.TileData.Any(x => !x.IsEmpty));
        DebugTools.Assert(data.TileData.Length == component.ChunkSize * component.ChunkSize);
        for (ushort x = 0; x < component.ChunkSize; x++)
        {
            for (ushort y = 0; y < component.ChunkSize; y++)
            {
                var tile = data.TileData[counter++];
                if (!chunk.TrySetTile(x, y, tile, out var oldTile, out _))
                    continue;

                var gridIndices = chunk.ChunkTileToGridTile((x, y));
                var newTileRef = new TileRef(uid, gridIndices, tile);
                _mapInternal.RaiseOnTileChanged(gridEnt, newTileRef, oldTile, index);
            }
        }

        DebugTools.Assert(chunk.Fixtures.SetEquals(data.Fixtures));

        // These should never refer to the same object
        DebugTools.AssertNotEqual(chunk.Fixtures, data.Fixtures);

        chunk.CachedBounds = data.CachedBounds!.Value;
        chunk.SuppressCollisionRegeneration = false;
    }

    private void OnGridGetState(EntityUid uid, MapGridComponent component, ref ComponentGetState args)
    {
        if (args.FromTick <= component.CreationTick)
        {
            GetFullState(uid, component, ref args);
            return;
        }

        Dictionary<Vector2i, ChunkDatum>? chunkData;
        var fromTick = args.FromTick;

        if (component.LastTileModifiedTick < fromTick)
        {
            chunkData = null;
        }
        else
        {
            chunkData = new Dictionary<Vector2i, ChunkDatum>();

            foreach (var (tick, indices) in component.ChunkDeletionHistory)
            {
                if (tick < fromTick && fromTick != GameTick.Zero)
                    continue;

                // Chunk may have been re-added sometime after it was deleted, but before deletion history was culled.
                if (!component.Chunks.TryGetValue(indices, out var chunk))
                {
                    chunkData.Add(indices, ChunkDatum.Empty);
                    continue;
                }

                if (chunk.LastTileModifiedTick < fromTick)
                    Log.Error($"Encountered un-deleted chunk with an old last-modified tick on grid {ToPrettyString(uid)}");
            }

            foreach (var (index, chunk) in GetMapChunks(uid, component))
            {
                if (chunk.LastTileModifiedTick < fromTick)
                    continue;

                var tileBuffer = new Tile[component.ChunkSize * (uint) component.ChunkSize];

                // Flatten the tile array.
                // NetSerializer doesn't do multi-dimensional arrays.
                // This is probably really expensive.
                for (var x = 0; x < component.ChunkSize; x++)
                {
                    for (var y = 0; y < component.ChunkSize; y++)
                    {
                        tileBuffer[x * component.ChunkSize + y] = chunk.GetTile((ushort)x, (ushort)y);
                    }
                }

                // The client needs to clone the fixture set instead of storing a reference.
                // TODO Game State
                // Force the client to serialize & de-serialize implicitly generated component states.
                var fixtures = chunk.Fixtures;
                if (_netManager.IsClient)
                    fixtures = new(fixtures);

                chunkData.Add(index, ChunkDatum.CreateModified(tileBuffer, fixtures, chunk.CachedBounds));
            }
        }

        args.State = new MapGridComponentDeltaState(component.ChunkSize, chunkData, component.LastTileModifiedTick);

#if DEBUG
        if (chunkData == null)
            return;

        HashSet<Vector2> keys = new();
        foreach (var (index, chunk) in chunkData)
        {
            if (chunk.IsDeleted())
                continue;

            DebugTools.Assert(keys.Add(index), "Duplicate chunk");
            DebugTools.Assert(chunk.TileData.Any(x => !x.IsEmpty), "Empty non-deleted chunk");
        }
#endif
    }

    private void GetFullState(EntityUid uid, MapGridComponent component, ref ComponentGetState args)
    {
        var chunkData = new Dictionary<Vector2i, ChunkDatum>();

        foreach (var (index, chunk) in GetMapChunks(uid, component))
        {
            var tileBuffer = new Tile[component.ChunkSize * (uint)component.ChunkSize];

            for (var x = 0; x < component.ChunkSize; x++)
            {
                for (var y = 0; y < component.ChunkSize; y++)
                {
                    tileBuffer[x * component.ChunkSize + y] = chunk.GetTile((ushort)x, (ushort)y);
                }
            }

            // The client needs to clone the fixture set instead of storing a reference.
            // TODO Game State
            // Force the client to serialize & de-serialize implicitly generated component states.
            var fixtures = chunk.Fixtures;
            if (_netManager.IsClient)
                fixtures = new(fixtures);

            chunkData.Add(index, ChunkDatum.CreateModified(tileBuffer, fixtures, chunk.CachedBounds));
        }

        args.State = new MapGridComponentState(component.ChunkSize, chunkData, component.LastTileModifiedTick);

#if DEBUG
        foreach (var chunk in chunkData.Values)
        {
            DebugTools.Assert(chunk.TileData!.Any(x => !x.IsEmpty));
        }
#endif
    }

    private void OnGridAdd(EntityUid uid, MapGridComponent component, ComponentAdd args)
    {
        var msg = new GridAddEvent(uid);
        RaiseLocalEvent(uid, msg, true);
    }

    private void OnGridInit(EntityUid uid, MapGridComponent component, ComponentInit args)
    {
        var xform = _xformQuery.GetComponent(uid);

        // Force networkedmapmanager to send it due to non-ECS legacy code.
        var curTick = _timing.CurTick;

        foreach (var chunk in component.Chunks.Values)
        {
            chunk.LastTileModifiedTick = curTick;
        }

        component.LastTileModifiedTick = curTick;

        if (xform.MapUid != null && xform.MapUid != uid)
            _transform.SetParent(uid, xform, xform.MapUid.Value);

        if (!_mapQuery.HasComponent(uid))
        {
            var aabb = GetWorldAABB(uid, component);

            if (TryComp<GridTreeComponent>(xform.MapUid, out var gridTree))
            {
                var proxy = gridTree.Tree.CreateProxy(in aabb, uint.MaxValue, (uid, _fixturesQuery.Comp(uid), component));
                DebugTools.Assert(component.MapProxy == DynamicTree.Proxy.Free);
                component.MapProxy = proxy;
            }

            if (TryComp<MovedGridsComponent>(xform.MapUid, out var movedGrids))
            {
                movedGrids.MovedGrids.Add(uid);
            }
        }

        var msg = new GridInitializeEvent(uid);
        RaiseLocalEvent(uid, msg, true);
    }

    private void OnGridStartup(EntityUid uid, MapGridComponent component, ComponentStartup args)
    {
        var msg = new GridStartupEvent(uid);
        RaiseLocalEvent(uid, msg, true);
    }

    private void OnGridRemove(EntityUid uid, MapGridComponent component, ComponentShutdown args)
    {
        Log.Info($"Removing grid {ToPrettyString(uid)}");
        if (TryComp(uid, out TransformComponent? xform) && xform.MapUid != null)
        {
            RemoveGrid(uid, component, xform.MapUid.Value);
        }

        component.MapProxy = DynamicTree.Proxy.Free;
        RaiseLocalEvent(uid, new GridRemovalEvent(uid), true);
    }

    private Box2 GetWorldAABB(EntityUid uid, MapGridComponent grid, TransformComponent? xform = null)
    {
        if (!Resolve(uid, ref xform))
            return new Box2();

        var (worldPos, worldRot) = _transform.GetWorldPositionRotation(xform);
        var aabb = grid.LocalAABB.Translated(worldPos);

        return new Box2Rotated(aabb, worldRot, worldPos).CalcBoundingBox();
    }

    private void AddGrid(EntityUid uid, MapGridComponent grid)
    {
        DebugTools.Assert(!_mapQuery.HasComponent(uid));
        var aabb = GetWorldAABB(uid, grid);

        if (!_xformQuery.TryGetComponent(uid, out var xform))
            return;

        if (TryComp<GridTreeComponent>(xform.MapUid, out var gridTree))
        {
            var proxy = gridTree.Tree.CreateProxy(in aabb, uint.MaxValue, (uid, _fixturesQuery.Comp(uid), grid));
            DebugTools.Assert(grid.MapProxy == DynamicTree.Proxy.Free);
            grid.MapProxy = proxy;
        }

        if (TryComp<MovedGridsComponent>(xform.MapUid, out var movedGrids))
        {
            movedGrids.MovedGrids.Add(uid);
        }
    }

    private void RemoveGrid(EntityUid uid, MapGridComponent grid, EntityUid mapUid)
    {
        if (grid.MapProxy != DynamicTree.Proxy.Free && TryComp<GridTreeComponent>(mapUid, out var gridTree))
        {
            gridTree.Tree.DestroyProxy(grid.MapProxy);
        }

        grid.MapProxy = DynamicTree.Proxy.Free;

        if (TryComp<MovedGridsComponent>(mapUid, out var movedGrids))
        {
            movedGrids.MovedGrids.Remove(uid);
        }
    }

    private void RemoveChunk(EntityUid uid, MapGridComponent grid, Vector2i origin)
    {
        if (!grid.Chunks.TryGetValue(origin, out var chunk))
            return;

        if (_netManager.IsServer)
            grid.ChunkDeletionHistory.Add((_timing.CurTick, chunk.Indices));

        chunk.Fixtures.Clear();
        grid.Chunks.Remove(origin);

        if (grid.Chunks.Count == 0)
            RaiseLocalEvent(uid, new EmptyGridEvent { GridId = uid }, true);
    }

    /// <summary>
    /// Regenerates the chunk local bounds of this chunk.
    /// </summary>
    private void RegenerateCollision(EntityUid uid, MapGridComponent grid, MapChunk mapChunk)
    {
        RegenerateCollision(uid, grid, new HashSet<MapChunk> { mapChunk });
    }

    /// <summary>
    /// Regenerate collision for multiple chunks at once; faster than doing it individually.
    /// </summary>
    internal void RegenerateCollision(EntityUid uid, MapGridComponent grid, IReadOnlySet<MapChunk> chunks)
    {
        if (HasComp<MapComponent>(uid))
        {
            ClearEmptyMapChunks(uid, grid, chunks);
            return;
        }

        var chunkRectangles = new Dictionary<MapChunk, List<Box2i>>(chunks.Count);
        var removedChunks = new List<MapChunk>();

        foreach (var mapChunk in chunks)
        {
            // Even if the chunk is still removed still need to make sure bounds are updated (for now...)
            // generate collision rectangles for this chunk based on filled tiles.
            GridChunkPartition.PartitionChunk(mapChunk, out var localBounds, out var rectangles);
            mapChunk.CachedBounds = localBounds;

            if (mapChunk.FilledTiles > 0)
                chunkRectangles.Add(mapChunk, rectangles);
            else
            {
                // Gone. Reduced to atoms
                // Need to do this before RemoveChunk because it clears fixtures.
                FixturesComponent? manager = null;
                PhysicsComponent? body = null;
                TransformComponent? xform = null;

                foreach (var id in mapChunk.Fixtures)
                {
                    mapChunk.Fixtures.Remove(id);
                    _fixtures.DestroyFixture(uid, id, false, manager: manager, body: body, xform: xform);
                }

                RemoveChunk(uid, grid, mapChunk.Indices);
                DebugTools.AssertEqual(mapChunk.Fixtures.Count, 0);
                removedChunks.Add(mapChunk);
            }
        }

        RegenerateAabb(grid);

        // May have been deleted from the bulk update above!
        if (Deleted(uid))
            return;

        _physics.WakeBody(uid);
        OnGridBoundsChange(uid, grid);
        var ev = new RegenerateGridBoundsEvent(uid, chunkRectangles, removedChunks);
        RaiseLocalEvent(ref ev);
    }

    private void RegenerateAabb(MapGridComponent grid)
    {
        grid.LocalAABB = new Box2();

        foreach (var chunk in grid.Chunks.Values)
        {
            var chunkBounds = chunk.CachedBounds;

            if (chunkBounds.Size.Equals(Vector2i.Zero))
                continue;

            if (grid.LocalAABB.Size == Vector2.Zero)
            {
                var gridBounds = chunkBounds.Translated(chunk.Indices * chunk.ChunkSize);
                grid.LocalAABB = gridBounds;
            }
            else
            {
                var gridBounds = chunkBounds.Translated(chunk.Indices * chunk.ChunkSize);
                grid.LocalAABB = grid.LocalAABB.Union(gridBounds);
            }
        }
    }

    /// <summary>
    /// Variation of <see cref="RegenerateCollision(Robust.Shared.GameObjects.EntityUid,Robust.Shared.Map.Components.MapGridComponent,Robust.Shared.Map.MapChunk)"/>
    /// that only simply removes empty chunks. Intended for use with "planet-maps", which have no grid fixtures.
    /// </summary>
    private void ClearEmptyMapChunks(EntityUid uid, MapGridComponent grid, IReadOnlySet<MapChunk> modified)
    {
        foreach (var chunk in modified)
        {
            DebugTools.Assert(chunk.FilledTiles >= 0);
            if (chunk.FilledTiles > 0)
                continue;

            DebugTools.AssertEqual(chunk.Fixtures.Count, 0, "maps should not have grid-chunk fixtures");
            RemoveChunk(uid, grid, chunk.Indices);
        }
    }

    #region TileAccess

    public TileRef GetTileRef(Entity<MapGridComponent> grid, MapCoordinates coords)
    {
        return GetTileRef(grid.Owner, grid.Comp, coords);
    }

    public TileRef GetTileRef(EntityUid uid, MapGridComponent grid, MapCoordinates coords)
    {
        return GetTileRef(uid, grid, CoordinatesToTile(uid, grid, coords));
    }

    public TileRef GetTileRef(Entity<MapGridComponent> grid, EntityCoordinates coords)
    {
        return GetTileRef(grid.Owner, grid.Comp, coords);
    }

    public TileRef GetTileRef(EntityUid uid, MapGridComponent grid, EntityCoordinates coords)
    {
        return GetTileRef(uid, grid, CoordinatesToTile(uid, grid, coords));
    }

    public TileRef GetTileRef(Entity<MapGridComponent> grid, Vector2i tileCoordinates)
    {
        return GetTileRef(grid.Owner, grid.Comp, tileCoordinates);
    }

    public TileRef GetTileRef(EntityUid uid, MapGridComponent grid, Vector2i tileCoordinates)
    {
        var chunkIndices = GridTileToChunkIndices(uid, grid, tileCoordinates);

        if (!grid.Chunks.TryGetValue(chunkIndices, out var output))
        {
            // Chunk doesn't exist, return a tileRef to an empty (space) tile.
            return new TileRef(uid, tileCoordinates.X, tileCoordinates.Y, default);
        }

        var chunkTileIndices = output.GridTileToChunkTile(tileCoordinates);
        return GetTileRef(uid, grid, output, (ushort)chunkTileIndices.X, (ushort)chunkTileIndices.Y);
    }

    /// <summary>
    ///     Returns the tile at the given chunk indices.
    /// </summary>
    /// <param name="mapChunk"></param>
    /// <param name="xIndex">The X tile index relative to the chunk origin.</param>
    /// <param name="yIndex">The Y tile index relative to the chunk origin.</param>
    /// <returns>A reference to a tile.</returns>
    internal TileRef GetTileRef(EntityUid uid, MapGridComponent grid, MapChunk mapChunk, ushort xIndex, ushort yIndex)
    {
        if (xIndex >= mapChunk.ChunkSize)
            throw new ArgumentOutOfRangeException(nameof(xIndex), "Tile indices out of bounds.");

        if (yIndex >= mapChunk.ChunkSize)
            throw new ArgumentOutOfRangeException(nameof(yIndex), "Tile indices out of bounds.");

        var indices = mapChunk.ChunkTileToGridTile(new Vector2i(xIndex, yIndex));
        return new TileRef(uid, indices, mapChunk.GetTile(xIndex, yIndex));
    }

    public IEnumerable<TileRef> GetAllTiles(EntityUid uid, MapGridComponent grid, bool ignoreEmpty = true)
    {
        foreach (var chunk in grid.Chunks.Values)
        {
            for (ushort x = 0; x < grid.ChunkSize; x++)
            {
                for (ushort y = 0; y < grid.ChunkSize; y++)
                {
                    var tile = chunk.GetTile(x, y);

                    if (ignoreEmpty && tile.IsEmpty)
                        continue;

                    var (gridX, gridY) = new Vector2i(x, y) + chunk.Indices * grid.ChunkSize;
                    yield return new TileRef(uid, gridX, gridY, tile);
                }
            }
        }
    }

    public GridTileEnumerator GetAllTilesEnumerator(EntityUid uid, MapGridComponent grid, bool ignoreEmpty = true)
    {
        return new GridTileEnumerator(uid, grid.Chunks.GetEnumerator(), grid.ChunkSize, ignoreEmpty);
    }

    public void SetTile(Entity<MapGridComponent> grid, EntityCoordinates coordinates, Tile tile)
    {
        SetTile(grid.Owner, grid.Comp, coordinates, tile);
    }

    public void SetTile(Entity<MapGridComponent> grid, Vector2i gridIndices, Tile tile)
    {
        SetTile(grid.Owner, grid.Comp, gridIndices, tile);
    }

    public void SetTiles(Entity<MapGridComponent> grid, List<(Vector2i GridIndices, Tile Tile)> tiles)
    {
        SetTiles(grid.Owner, grid.Comp, tiles);
    }

    public void SetTile(EntityUid uid, MapGridComponent grid, EntityCoordinates coords, Tile tile)
    {
        var localTile = CoordinatesToTile(uid, grid, coords);
        SetTile(uid, grid, new Vector2i(localTile.X, localTile.Y), tile);
    }

    public void SetTile(EntityUid uid, MapGridComponent grid, Vector2i gridIndices, Tile tile)
    {
        var chunkIndex = GridTileToChunkIndices(uid, grid, gridIndices);
        if (!grid.Chunks.TryGetValue(chunkIndex, out var chunk))
        {
            if (tile.IsEmpty)
                return;

            grid.Chunks[chunkIndex] = chunk = new MapChunk(chunkIndex.X, chunkIndex.Y, grid.ChunkSize)
            {
                LastTileModifiedTick = _timing.CurTick
            };
        }

        var offset = chunk.GridTileToChunkTile(gridIndices);
        SetChunkTile(uid, grid, chunk, (ushort)offset.X, (ushort)offset.Y, tile);
    }

    public void SetTiles(EntityUid uid, MapGridComponent grid, List<(Vector2i GridIndices, Tile Tile)> tiles)
    {
        if (tiles.Count == 0)
            return;

        var modified = new HashSet<MapChunk>(Math.Max(1, tiles.Count / grid.ChunkSize));

        foreach (var (gridIndices, tile) in tiles)
        {
            var chunkIndex = GridTileToChunkIndices(uid, grid, gridIndices);
            if (!grid.Chunks.TryGetValue(chunkIndex, out var chunk))
            {
                if (tile.IsEmpty)
                    continue;

                grid.Chunks[chunkIndex] = chunk = new MapChunk(chunkIndex.X, chunkIndex.Y, grid.ChunkSize)
                {
                    LastTileModifiedTick = _timing.CurTick
                };
            }

            var offset = chunk.GridTileToChunkTile(gridIndices);
            chunk.SuppressCollisionRegeneration = true;
            if (SetChunkTile(uid, grid, chunk, (ushort)offset.X, (ushort)offset.Y, tile))
                modified.Add(chunk);
        }

        foreach (var chunk in modified)
        {
            chunk.SuppressCollisionRegeneration = false;
        }

        RegenerateCollision(uid, grid, modified);
    }

    public TilesEnumerator GetLocalTilesEnumerator(EntityUid uid, MapGridComponent grid, Box2 aabb,
        bool ignoreEmpty = true,
        Predicate<TileRef>? predicate = null)
    {
        var enumerator = new TilesEnumerator(this, ignoreEmpty, predicate, uid, grid, aabb);
        return enumerator;
    }

    public TilesEnumerator GetTilesEnumerator(EntityUid uid, MapGridComponent grid, Box2 aabb, bool ignoreEmpty = true,
        Predicate<TileRef>? predicate = null)
    {
        var invMatrix = _transform.GetInvWorldMatrix(uid);
        var localAABB = invMatrix.TransformBox(aabb);
        var enumerator = new TilesEnumerator(this, ignoreEmpty, predicate, uid, grid, localAABB);
        return enumerator;
    }

    public TilesEnumerator GetTilesEnumerator(EntityUid uid, MapGridComponent grid, Box2Rotated bounds, bool ignoreEmpty = true,
        Predicate<TileRef>? predicate = null)
    {
        var invMatrix = _transform.GetInvWorldMatrix(uid);
        var localAABB = invMatrix.TransformBox(bounds);
        var enumerator = new TilesEnumerator(this, ignoreEmpty, predicate, uid, grid, localAABB);
        return enumerator;
    }

    public IEnumerable<TileRef> GetLocalTilesIntersecting(EntityUid uid, MapGridComponent grid, Box2 localAABB, bool ignoreEmpty = true,
        Predicate<TileRef>? predicate = null)
    {
        var enumerator = new TilesEnumerator(this, ignoreEmpty, predicate, uid, grid, localAABB);

        while (enumerator.MoveNext(out var tileRef))
        {
            yield return tileRef;
        }
    }

    public IEnumerable<TileRef> GetLocalTilesIntersecting(EntityUid uid, MapGridComponent grid, Box2Rotated localArea, bool ignoreEmpty = true,
        Predicate<TileRef>? predicate = null)
    {
        var localAABB = localArea.CalcBoundingBox();

        var enumerator = new TilesEnumerator(this, ignoreEmpty, predicate, uid, grid, localAABB);

        while (enumerator.MoveNext(out var tileRef))
        {
            yield return tileRef;
        }
    }

    public IEnumerable<TileRef> GetTilesIntersecting(EntityUid uid, MapGridComponent grid, Box2Rotated worldArea, bool ignoreEmpty = true,
        Predicate<TileRef>? predicate = null)
    {
        var matrix = _transform.GetInvWorldMatrix(uid);
        var localArea = matrix.TransformBox(worldArea);

        var enumerator = new TilesEnumerator(this, ignoreEmpty, predicate, uid, grid, localArea);

        while (enumerator.MoveNext(out var tileRef))
        {
            yield return tileRef;
        }
    }

    public IEnumerable<TileRef> GetTilesIntersecting(EntityUid uid, MapGridComponent grid, Box2 worldArea, bool ignoreEmpty = true,
        Predicate<TileRef>? predicate = null)
    {
        var matrix = _transform.GetInvWorldMatrix(uid);
        var localArea = matrix.TransformBox(worldArea);

        var enumerator = new TilesEnumerator(this, ignoreEmpty, predicate, uid, grid, localArea);

        while (enumerator.MoveNext(out var tileRef))
        {
            yield return tileRef;
        }
    }

    public IEnumerable<TileRef> GetLocalTilesIntersecting(EntityUid uid, MapGridComponent grid, Circle localCircle, bool ignoreEmpty = true,
        Predicate<TileRef>? predicate = null)
    {
        var aabb = new Box2(localCircle.Position.X - localCircle.Radius, localCircle.Position.Y - localCircle.Radius,
            localCircle.Position.X + localCircle.Radius, localCircle.Position.Y + localCircle.Radius);

        var tileEnumerator = GetLocalTilesEnumerator(uid, grid, aabb, ignoreEmpty, predicate);

        while (tileEnumerator.MoveNext(out var tile))
        {
            var tileCenter = tile.GridIndices + grid.TileSizeHalfVector;
            var direction = tileCenter - localCircle.Position;

            if (direction.IsShorterThanOrEqualTo(localCircle.Radius))
            {
                yield return tile;
            }
        }
    }

    public IEnumerable<TileRef> GetTilesIntersecting(EntityUid uid, MapGridComponent grid, Circle worldArea, bool ignoreEmpty = true,
        Predicate<TileRef>? predicate = null)
    {
        var aabb = new Box2(worldArea.Position.X - worldArea.Radius, worldArea.Position.Y - worldArea.Radius,
            worldArea.Position.X + worldArea.Radius, worldArea.Position.Y + worldArea.Radius);
        var circleGridPos = new EntityCoordinates(uid, WorldToLocal(uid, grid, worldArea.Position));

        foreach (var tile in GetTilesIntersecting(uid, grid, aabb, ignoreEmpty, predicate))
        {
            var local = GridTileToLocal(uid, grid, tile.GridIndices);

            if (!local.TryDistance(EntityManager, _transform, circleGridPos, out var distance))
            {
                continue;
            }

            if (distance <= worldArea.Radius)
            {
                yield return tile;
            }
        }
    }

    private bool TryGetTile(EntityUid uid, MapGridComponent grid, Vector2i indices, bool ignoreEmpty, [NotNullWhen(true)] out TileRef? tileRef, Predicate<TileRef>? predicate = null)
    {
        // Similar to TryGetTileRef but for the tiles intersecting iterators.
        var gridChunk = GridTileToChunkIndices(uid, grid, indices);

        if (grid.Chunks.TryGetValue(gridChunk, out var chunk))
        {
            var chunkTile = chunk.GridTileToChunkTile(indices);
            var tile = GetTileRef(uid, grid, chunk, (ushort)chunkTile.X, (ushort)chunkTile.Y);

            if (ignoreEmpty && tile.Tile.IsEmpty)
            {
                tileRef = null;
                return false;
            }

            if (predicate == null || predicate(tile))
            {
                tileRef = tile;
                return true;
            }
        }
        else if (!ignoreEmpty)
        {
            var tile = new TileRef(uid, indices.X, indices.Y, Tile.Empty);

            if (predicate == null || predicate(tile))
            {
                tileRef = tile;
                return true;
            }
        }

        tileRef = null;
        return false;
    }

    #endregion TileAccess

    #region ChunkAccess

    internal MapChunk GetOrAddChunk(EntityUid uid, MapGridComponent grid, int xIndex, int yIndex)
    {
        return GetOrAddChunk(uid, grid, new Vector2i(xIndex, yIndex));
    }

    internal bool TryGetChunk(EntityUid uid, MapGridComponent grid, Vector2i chunkIndices, [NotNullWhen(true)] out MapChunk? chunk)
    {
        return grid.Chunks.TryGetValue(chunkIndices, out chunk);
    }

    internal MapChunk GetOrAddChunk(EntityUid uid, MapGridComponent grid, Vector2i chunkIndices)
    {
        if (grid.Chunks.TryGetValue(chunkIndices, out var output))
            return output;

        var newChunk = new MapChunk(chunkIndices.X, chunkIndices.Y, grid.ChunkSize)
        {
            LastTileModifiedTick = _timing.CurTick
        };

        return grid.Chunks[chunkIndices] = newChunk;
    }

    public bool HasChunk(EntityUid uid, MapGridComponent grid, Vector2i chunkIndices)
    {
        return grid.Chunks.ContainsKey(chunkIndices);
    }

    internal IReadOnlyDictionary<Vector2i, MapChunk> GetMapChunks(EntityUid uid, MapGridComponent grid)
    {
        return grid.Chunks;
    }

    internal ChunkEnumerator GetMapChunks(EntityUid uid, MapGridComponent grid, Box2 worldAABB)
    {
        var localAABB = _transform.GetInvWorldMatrix(uid).TransformBox(worldAABB);
        return GetLocalMapChunks(uid, grid, localAABB);
    }

    internal ChunkEnumerator GetMapChunks(EntityUid uid, MapGridComponent grid, Box2Rotated worldArea)
    {
        var matrix = _transform.GetInvWorldMatrix(uid);
        var localArea = matrix.TransformBox(worldArea);
        return GetLocalMapChunks(uid, grid, localArea);
    }

    internal ChunkEnumerator GetLocalMapChunks(EntityUid uid, MapGridComponent grid, Box2 localAABB)
    {
        Box2 compAABB;

        // The entire area intersects.
        if (_mapQuery.HasComponent(uid))
        {
            compAABB = localAABB;
        }
        else
        {
            compAABB = grid.LocalAABB.Intersect(localAABB);
        }

        return new ChunkEnumerator(grid.Chunks, compAABB, grid.ChunkSize);
    }

    #endregion ChunkAccess

    #region SnapGridAccess

    public int AnchoredEntityCount(EntityUid uid, MapGridComponent grid, Vector2i pos)
    {
        var gridChunkPos = GridTileToChunkIndices(uid, grid, pos);

        if (!grid.Chunks.TryGetValue(gridChunkPos, out var chunk))
            return 0;

        var (x, y) = chunk.GridTileToChunkTile(pos);
        return chunk.GetSnapGrid((ushort)x, (ushort)y)?.Count ?? 0; // ?
    }

    public IEnumerable<EntityUid> GetAnchoredEntities(Entity<MapGridComponent> grid, MapCoordinates coords)
    {
        return GetAnchoredEntities(grid.Owner, grid.Comp, coords);
    }

    public IEnumerable<EntityUid> GetAnchoredEntities(EntityUid uid, MapGridComponent grid, MapCoordinates coords)
    {
        return GetAnchoredEntities(uid, grid, TileIndicesFor(uid, grid, coords));
    }

    public IEnumerable<EntityUid> GetAnchoredEntities(Entity<MapGridComponent> grid, EntityCoordinates coords)
    {
        return GetAnchoredEntities(grid.Owner, grid.Comp, coords);
    }

    public IEnumerable<EntityUid> GetAnchoredEntities(EntityUid uid, MapGridComponent grid, EntityCoordinates coords)
    {
        return GetAnchoredEntities(uid, grid, TileIndicesFor(uid, grid, coords));
    }

    public IEnumerable<EntityUid> GetAnchoredEntities(Entity<MapGridComponent> grid, Vector2i pos)
    {
        return GetAnchoredEntities(grid.Owner, grid.Comp, pos);
    }

    public IEnumerable<EntityUid> GetAnchoredEntities(EntityUid uid, MapGridComponent grid, Vector2i pos)
    {
        // Because some content stuff checks neighboring tiles (which may not actually exist) we won't just
        // create an entire chunk for it.
        var gridChunkPos = GridTileToChunkIndices(uid, grid, pos);

        if (!grid.Chunks.TryGetValue(gridChunkPos, out var chunk))
            return Enumerable.Empty<EntityUid>();

        var chunkTile = chunk.GridTileToChunkTile(pos);
        return chunk.GetSnapGridCell((ushort)chunkTile.X, (ushort)chunkTile.Y);
    }

    public void GetAnchoredEntities(Entity<MapGridComponent> grid, Vector2i pos, List<EntityUid> list)
    {
        var gridChunkPos = GridTileToChunkIndices(grid.Owner, grid.Comp, pos);
        if (!grid.Comp.Chunks.TryGetValue(gridChunkPos, out var chunk))
            return;

        var chunkTile = chunk.GridTileToChunkTile(pos);
        var anchored = chunk.GetSnapGrid((ushort) chunkTile.X, (ushort) chunkTile.Y);
        if (anchored != null)
            list.AddRange(anchored);
    }

    public AnchoredEntitiesEnumerator GetAnchoredEntitiesEnumerator(EntityUid uid, MapGridComponent grid, Vector2i pos)
    {
        var gridChunkPos = GridTileToChunkIndices(uid, grid, pos);

        if (!grid.Chunks.TryGetValue(gridChunkPos, out var chunk)) return AnchoredEntitiesEnumerator.Empty;

        var chunkTile = chunk.GridTileToChunkTile(pos);
        var snapgrid = chunk.GetSnapGrid((ushort)chunkTile.X, (ushort)chunkTile.Y);

        return snapgrid == null
            ? AnchoredEntitiesEnumerator.Empty
            : new AnchoredEntitiesEnumerator(snapgrid.GetEnumerator());
    }

    public IEnumerable<EntityUid> GetLocalAnchoredEntities(EntityUid uid, MapGridComponent grid, Box2 localAABB)
    {
        var enumerator = new TilesEnumerator(this, true, null, uid, grid, localAABB);

        while (enumerator.MoveNext(out var tileRef))
        {
            var anchoredEnumerator = GetAnchoredEntitiesEnumerator(uid, grid, tileRef.GridIndices);

            while (anchoredEnumerator.MoveNext(out var ent))
            {
                yield return ent.Value;
            }
        }
    }

    public IEnumerable<EntityUid> GetAnchoredEntities(EntityUid uid, MapGridComponent grid, Box2 worldAABB)
    {
        var invWorldMatrix = _transform.GetInvWorldMatrix(uid);
        var localAABB = invWorldMatrix.TransformBox(worldAABB);
        var enumerator = new TilesEnumerator(this, true, null, uid, grid, localAABB);

        while (enumerator.MoveNext(out var tileRef))
        {
            var anchoredEnumerator = GetAnchoredEntitiesEnumerator(uid, grid, tileRef.GridIndices);

            while (anchoredEnumerator.MoveNext(out var ent))
            {
                yield return ent.Value;
            }
        }
    }

    public IEnumerable<EntityUid> GetAnchoredEntities(EntityUid uid, MapGridComponent grid, Box2Rotated worldBounds)
    {
        foreach (var tile in GetTilesIntersecting(uid, grid, worldBounds))
        {
            foreach (var ent in GetAnchoredEntities(uid, grid, tile.GridIndices))
            {
                yield return ent;
            }
        }
    }

    public Vector2i TileIndicesFor(EntityUid uid, MapGridComponent grid, EntityCoordinates coords)
    {
#if DEBUG
        var mapId = _xformQuery.GetComponent(uid).MapID;
        DebugTools.Assert(mapId == _transform.GetMapId(coords));
#endif

        return SnapGridLocalCellFor(uid, grid, LocalToGrid(uid, grid, coords));
    }

    public Vector2i TileIndicesFor(Entity<MapGridComponent> grid, EntityCoordinates coords)
    {
        return TileIndicesFor(grid.Owner, grid.Comp, coords);
    }

    public Vector2i TileIndicesFor(EntityUid uid, MapGridComponent grid, MapCoordinates worldPos)
    {
#if DEBUG
        var mapId = _xformQuery.GetComponent(uid).MapID;
        DebugTools.Assert(mapId == worldPos.MapId);
#endif

        var localPos = WorldToLocal(uid, grid, worldPos.Position);
        return SnapGridLocalCellFor(uid, grid, localPos);
    }

    public Vector2i TileIndicesFor(Entity<MapGridComponent> grid, MapCoordinates coords)
    {
        return TileIndicesFor(grid.Owner, grid.Comp, coords);
    }

    private Vector2i SnapGridLocalCellFor(EntityUid uid, MapGridComponent grid, Vector2 localPos)
    {
        var x = (int)Math.Floor(localPos.X / grid.TileSize);
        var y = (int)Math.Floor(localPos.Y / grid.TileSize);
        return new Vector2i(x, y);
    }

    public bool IsAnchored(EntityUid uid, MapGridComponent grid, EntityCoordinates coords, EntityUid euid)
    {
        var tilePos = TileIndicesFor(uid, grid, coords);

        if (!TryChunkAndOffsetForTile(uid, grid, tilePos, out var chunk, out var chunkTile))
            return false;

        var snapgrid = chunk.GetSnapGrid((ushort)chunkTile.X, (ushort)chunkTile.Y);
        return snapgrid?.Contains(euid) == true;
    }

    public bool AddToSnapGridCell(EntityUid gridUid, MapGridComponent grid, Vector2i pos, EntityUid euid)
    {
        if (!TryChunkAndOffsetForTile(gridUid, grid, pos, out var chunk, out var chunkTile))
            return false;

        if (chunk.GetTile((ushort)chunkTile.X, (ushort)chunkTile.Y).IsEmpty)
            return false;

        chunk.AddToSnapGridCell((ushort)chunkTile.X, (ushort)chunkTile.Y, euid);
        return true;
    }

    public bool AddToSnapGridCell(EntityUid gridUid, MapGridComponent grid, EntityCoordinates coords, EntityUid euid)
    {
        return AddToSnapGridCell(gridUid, grid, TileIndicesFor(gridUid, grid, coords), euid);
    }

    public void RemoveFromSnapGridCell(EntityUid gridUid, MapGridComponent grid, Vector2i pos, EntityUid euid)
    {
        var gridChunkIndices = GridTileToChunkIndices(gridUid, grid, pos);

        if (!grid.Chunks.TryGetValue(gridChunkIndices, out var chunk))
            return;

        var chunkTile = chunk.GridTileToChunkTile(pos);
        chunk.RemoveFromSnapGridCell((ushort)chunkTile.X, (ushort)chunkTile.Y, euid);
    }

    public void RemoveFromSnapGridCell(EntityUid gridUid, MapGridComponent grid, EntityCoordinates coords, EntityUid euid)
    {
        RemoveFromSnapGridCell(gridUid, grid, TileIndicesFor(gridUid, grid, coords), euid);
    }

    private bool TryChunkAndOffsetForTile(EntityUid uid, MapGridComponent grid, Vector2i pos,
        [NotNullWhen(true)]out MapChunk? chunk, out Vector2i offset)
    {
        var gridChunkIndices = GridTileToChunkIndices(uid, grid, pos);
        if (!grid.Chunks.TryGetValue(gridChunkIndices, out chunk))
        {
            offset = default;
            return false;
        }

        offset = chunk.GridTileToChunkTile(pos);
        return true;
    }

    public IEnumerable<EntityUid> GetInDir(EntityUid uid, MapGridComponent grid, EntityCoordinates position, Direction dir)
    {
        var pos = GetDirection(TileIndicesFor(uid, grid, position), dir);
        return GetAnchoredEntities(uid, grid, pos);
    }

    public IEnumerable<EntityUid> GetOffset(EntityUid uid, MapGridComponent grid, EntityCoordinates coords, Vector2i offset)
    {
        var pos = TileIndicesFor(uid, grid, coords) + offset;
        return GetAnchoredEntities(uid, grid, pos);
    }

    public IEnumerable<EntityUid> GetLocal(EntityUid uid, MapGridComponent grid, EntityCoordinates coords)
    {
        return GetAnchoredEntities(uid, grid, TileIndicesFor(uid, grid, coords));
    }

    public EntityCoordinates DirectionToGrid(EntityUid uid, MapGridComponent grid, EntityCoordinates coords, Direction direction)
    {
        return GridTileToLocal(uid, grid, GetDirection(TileIndicesFor(uid, grid, coords), direction));
    }

    public IEnumerable<EntityUid> GetCardinalNeighborCells(EntityUid uid, MapGridComponent grid, EntityCoordinates coords)
    {
        var position = TileIndicesFor(uid, grid, coords);
        foreach (var cell in GetAnchoredEntities(uid, grid, position))
            yield return cell;
        foreach (var cell in GetAnchoredEntities(uid, grid, position + new Vector2i(0, 1)))
            yield return cell;
        foreach (var cell in GetAnchoredEntities(uid, grid, position + new Vector2i(0, -1)))
            yield return cell;
        foreach (var cell in GetAnchoredEntities(uid, grid, position + new Vector2i(1, 0)))
            yield return cell;
        foreach (var cell in GetAnchoredEntities(uid, grid, position + new Vector2i(-1, 0)))
            yield return cell;
    }

    public IEnumerable<EntityUid> GetCellsInSquareArea(EntityUid uid, MapGridComponent grid, EntityCoordinates coords, int n)
    {
        var position = TileIndicesFor(uid, grid, coords);

        for (var y = -n; y <= n; ++y)
        for (var x = -n; x <= n; ++x)
        {
            var enumerator = GetAnchoredEntitiesEnumerator(uid, grid, position + new Vector2i(x, y));

            while (enumerator.MoveNext(out var cell))
            {
                yield return cell.Value;
            }
        }
    }

    #endregion

    #region Transforms

    public Vector2 WorldToLocal(EntityUid uid, MapGridComponent grid, Vector2 posWorld)
    {
        var matrix = _transform.GetInvWorldMatrix(uid);
        return Vector2.Transform(posWorld, matrix);
    }

    public EntityCoordinates MapToGrid(EntityUid uid, MapCoordinates posWorld)
    {
        var mapId = _xformQuery.GetComponent(uid).MapID;

        if (posWorld.MapId != mapId)
            throw new ArgumentException(
                $"Grid {uid} is on map {mapId}, but coords are on map {posWorld.MapId}.",
                nameof(posWorld));

        if (!_gridQuery.TryGetComponent(uid, out var grid))
        {
            return new EntityCoordinates(GetMapOrInvalid(posWorld.MapId), new Vector2(posWorld.X, posWorld.Y));
        }

        return new EntityCoordinates(uid, WorldToLocal(uid, grid, posWorld.Position));
    }

    public Vector2 LocalToWorld(EntityUid uid, MapGridComponent grid, Vector2 posLocal)
    {
        var matrix = _transform.GetWorldMatrix(uid);
        return Vector2.Transform(posLocal, matrix);
    }

    public Vector2i WorldToTile(EntityUid uid, MapGridComponent grid, Vector2 posWorld)
    {
        var local = WorldToLocal(uid, grid, posWorld);
        var x = (int)Math.Floor(local.X / grid.TileSize);
        var y = (int)Math.Floor(local.Y / grid.TileSize);
        return new Vector2i(x, y);
    }

    public Vector2i LocalToTile(EntityUid uid, MapGridComponent grid, EntityCoordinates coordinates)
    {
        var position = LocalToGrid(uid, grid, coordinates);
        return new Vector2i((int) Math.Floor(position.X / grid.TileSize), (int) Math.Floor(position.Y / grid.TileSize));
    }

        public Vector2i CoordinatesToTile(EntityUid uid, MapGridComponent grid, MapCoordinates coords)
    {
#if DEBUG
        var mapId = _xformQuery.GetComponent(uid).MapID;
        DebugTools.Assert(mapId == coords.MapId);
#endif

        var local = WorldToLocal(uid, grid, coords.Position);

        var x = (int)Math.Floor(local.X / grid.TileSize);
        var y = (int)Math.Floor(local.Y / grid.TileSize);
        return new Vector2i(x, y);
    }

    public Vector2i CoordinatesToTile(EntityUid uid, MapGridComponent grid, EntityCoordinates coords)
    {
#if DEBUG
        var mapId = _xformQuery.GetComponent(uid).MapID;
        DebugTools.Assert(mapId == _transform.GetMapId(coords));
#endif
        var local = LocalToGrid(uid, grid, coords);

        var x = (int)Math.Floor(local.X / grid.TileSize);
        var y = (int)Math.Floor(local.Y / grid.TileSize);
        return new Vector2i(x, y);
    }

    public Vector2i LocalToChunkIndices(EntityUid uid, MapGridComponent grid, EntityCoordinates gridPos)
    {
        var local = LocalToGrid(uid, grid, gridPos);

        var x = (int)Math.Floor(local.X / (grid.TileSize * grid.ChunkSize));
        var y = (int)Math.Floor(local.Y / (grid.TileSize * grid.ChunkSize));
        return new Vector2i(x, y);
    }

    public Vector2 LocalToGrid(EntityUid uid, MapGridComponent grid, EntityCoordinates position)
    {
        return position.EntityId == uid
            ? position.Position
            : WorldToLocal(uid, grid, _transform.ToMapCoordinates(position).Position);
    }

    public bool CollidesWithGrid(EntityUid uid, MapGridComponent grid, Vector2i indices)
    {
        var chunkIndices = GridTileToChunkIndices(uid, grid, indices);
        if (!grid.Chunks.TryGetValue(chunkIndices, out var chunk))
            return false;

        var cTileIndices = chunk.GridTileToChunkTile(indices);
        return chunk.GetTile((ushort)cTileIndices.X, (ushort)cTileIndices.Y).TypeId != Tile.Empty.TypeId;
    }

    public Vector2i GridTileToChunkIndices(EntityUid uid, MapGridComponent grid, Vector2i gridTile)
        => GridTileToChunkIndices(grid, gridTile);

    public Vector2i GridTileToChunkIndices(MapGridComponent grid, Vector2i gridTile)
    {
        var x = (int)Math.Floor(gridTile.X / (float) grid.ChunkSize);
        var y = (int)Math.Floor(gridTile.Y / (float) grid.ChunkSize);

        return new Vector2i(x, y);
    }

    public EntityCoordinates GridTileToLocal(EntityUid uid, MapGridComponent grid, Vector2i gridTile)
    {
        var position = TileCenterToVector(uid, grid, gridTile);

        return new(uid, position);
    }

    /// <summary>
    /// Turns a gridtile origin into a Vector2, accounting for tile size.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 TileToVector(Entity<MapGridComponent> grid, Vector2i gridTile)
    {
        return new Vector2(gridTile.X * grid.Comp.TileSize, gridTile.Y * grid.Comp.TileSize);
    }

    /// <summary>
    /// Turns a gridtile center into a Vector2, accounting for tile size.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 TileCenterToVector(EntityUid uid, MapGridComponent grid, Vector2i gridTile)
    {
        return TileCenterToVector((uid, grid), gridTile);
    }

    /// <summary>
    /// Turns a gridtile center into a Vector2, accounting for tile size.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 TileCenterToVector(Entity<MapGridComponent> grid, Vector2i gridTile)
    {
        return new Vector2(gridTile.X * grid.Comp.TileSize, gridTile.Y * grid.Comp.TileSize) + grid.Comp.TileSizeHalfVector;
    }

    public Vector2 GridTileToWorldPos(EntityUid uid, MapGridComponent grid, Vector2i gridTile)
    {
        var locX = gridTile.X * grid.TileSize + (grid.TileSize / 2f);
        var locY = gridTile.Y * grid.TileSize + (grid.TileSize / 2f);

        return Vector2.Transform(new Vector2(locX, locY), _transform.GetWorldMatrix(uid));
    }

    public MapCoordinates GridTileToWorld(EntityUid uid, MapGridComponent grid, Vector2i gridTile)
    {
        var parentMapId = _xformQuery.GetComponent(uid).MapID;

        return new(GridTileToWorldPos(uid, grid, gridTile), parentMapId);
    }

    public bool TryGetTileRef(EntityUid uid, MapGridComponent grid, Vector2i indices, out TileRef tile)
    {
        var chunkIndices = GridTileToChunkIndices(uid, grid, indices);
        if (!grid.Chunks.TryGetValue(chunkIndices, out var chunk))
        {
            tile = default;
            return false;
        }

        var cTileIndices = chunk.GridTileToChunkTile(indices);
        tile = GetTileRef(uid, grid, chunk, (ushort)cTileIndices.X, (ushort)cTileIndices.Y);
        return true;
    }

    public bool TryGetTile(MapGridComponent grid, Vector2i indices, out Tile tile)
    {
        var chunkIndices = GridTileToChunkIndices(grid, indices);
        if (!grid.Chunks.TryGetValue(chunkIndices, out var chunk))
        {
            tile = default;
            return false;
        }

        var cTileIndices = chunk.GridTileToChunkTile(indices);
        tile = chunk.Tiles[cTileIndices.X, cTileIndices.Y];
        return true;
    }

    /// <summary>
    /// Attempts to get the <see cref="ITileDefinition"/> for the tile at the given grid indices. This will throw an
    /// exception if the tile at this location has no registered tile definition.
    /// </summary>
    public bool TryGetTileDef(MapGridComponent grid, Vector2i indices, [NotNullWhen(true)] out ITileDefinition? tileDef)
    {
        if (!TryGetTile(grid, indices, out var tile))
        {
            tileDef = null;
            return false;
        }

        tileDef = _tileMan[tile.TypeId];
        return true;
    }

    public bool TryGetTileRef(EntityUid uid, MapGridComponent grid, EntityCoordinates coords, out TileRef tile)
    {
        return TryGetTileRef(uid, grid, CoordinatesToTile(uid, grid, coords), out tile);
    }

    public bool TryGetTileRef(EntityUid uid, MapGridComponent grid, Vector2 worldPos, out TileRef tile)
    {
        return TryGetTileRef(uid, grid, WorldToTile(uid, grid, worldPos), out tile);
    }

    #endregion Transforms

    /// <summary>
    /// Calculate the world space AABB for this chunk.
    /// </summary>
    internal Box2 CalcWorldAABB(EntityUid uid, MapGridComponent grid, MapChunk mapChunk)
    {
        var (position, rotation) =
            _transform.GetWorldPositionRotation(uid);

        var chunkPosition = mapChunk.Indices;
        var tileScale = grid.TileSize;
        var chunkScale = mapChunk.ChunkSize;

        var worldPos = position + rotation.RotateVec(chunkPosition * tileScale * chunkScale);

        return new Box2Rotated(
            ((Box2)mapChunk.CachedBounds
                .Scale(tileScale))
            .Translated(worldPos),
            rotation, worldPos).CalcBoundingBox();
    }

    private void OnTileModified(EntityUid uid, MapGridComponent grid, MapChunk mapChunk, Vector2i tileIndices, Tile newTile, Tile oldTile,
        bool shapeChanged)
    {
        // As the collision regeneration can potentially delete the chunk we'll notify of the tile changed first.
        var gridTile = mapChunk.ChunkTileToGridTile(tileIndices);
        mapChunk.LastTileModifiedTick = _timing.CurTick;
        grid.LastTileModifiedTick = _timing.CurTick;
        Dirty(uid, grid);

        // The map serializer currently sets tiles of unbound grids as part of the deserialization process
        // It properly sets SuppressOnTileChanged so that the event isn't spammed for every tile on the grid.
        // ParentMapId is not able to be accessed on unbound grids, so we can't even call this function for unbound grids.
        if (!MapManager.SuppressOnTileChanged)
        {
            var newTileRef = new TileRef(uid, gridTile, newTile);
            _mapInternal.RaiseOnTileChanged((uid, grid), newTileRef, oldTile, mapChunk.Indices);
        }

        if (shapeChanged && !mapChunk.SuppressCollisionRegeneration)
        {
            RegenerateCollision(uid, grid, mapChunk);
        }
    }

    /// <summary>
    /// Iterates the local tiles of the specified data.
    /// </summary>
    public struct TilesEnumerator
    {
        private readonly SharedMapSystem _mapSystem;

        private readonly EntityUid _uid;
        private readonly MapGridComponent _grid;
        private readonly bool _ignoreEmpty;
        private readonly Predicate<TileRef>? _predicate;

        private readonly int _lowerY;
        private readonly int _upperX;
        private readonly int _upperY;

        private int _x;
        private int _y;

        public TilesEnumerator(
            SharedMapSystem mapSystem,
            bool ignoreEmpty,
            Predicate<TileRef>? predicate,
            EntityUid uid,
            MapGridComponent grid,
            Box2 aabb)
        {
            _mapSystem = mapSystem;

            _uid = uid;
            _grid = grid;
            _ignoreEmpty = ignoreEmpty;
            _predicate = predicate;

            // TODO: Should move the intersecting calls onto mapmanager system and then allow people to pass in xform / xformquery
            // that way we can avoid the GetComp here.
            var gridTileLb = new Vector2i((int)Math.Floor(aabb.Left), (int)Math.Floor(aabb.Bottom));
            // If we have 20.1 we want to include that tile but if we have 20 then we don't.
            var gridTileRt = new Vector2i((int)Math.Ceiling(aabb.Right), (int)Math.Ceiling(aabb.Top));

            _x = gridTileLb.X;
            _y = gridTileLb.Y;
            _lowerY = gridTileLb.Y;
            _upperX = gridTileRt.X;
            _upperY = gridTileRt.Y;
        }

        public bool MoveNext(out TileRef tile)
        {
            if (_x >= _upperX)
            {
                tile = TileRef.Zero;
                return false;
            }

            var gridTile = new Vector2i(_x, _y);

            _y++;

            if (_y >= _upperY)
            {
                _x++;
                _y = _lowerY;
            }

            var gridChunk = _mapSystem.GridTileToChunkIndices(_uid, _grid, gridTile);

            if (_grid.Chunks.TryGetValue(gridChunk, out var chunk))
            {
                var chunkTile = chunk.GridTileToChunkTile(gridTile);
                tile = _mapSystem.GetTileRef(_uid, _grid, chunk, (ushort)chunkTile.X, (ushort)chunkTile.Y);

                if (_ignoreEmpty && tile.Tile.IsEmpty)
                    return MoveNext(out tile);

                if (_predicate == null || _predicate(tile))
                {
                    return true;
                }
            }
            else if (!_ignoreEmpty)
            {
                tile = new TileRef(_uid, gridTile.X, gridTile.Y, Tile.Empty);

                if (_predicate == null || _predicate(tile))
                {
                    return true;
                }
            }

            return MoveNext(out tile);
        }
    }
}
