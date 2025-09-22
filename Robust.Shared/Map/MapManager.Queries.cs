using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Map.Enumerators;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Shapes;

namespace Robust.Shared.Map;

internal partial class MapManager
{
    private bool IsIntersecting<T>(
        ChunkEnumerator enumerator,
        T shape,
        Transform shapeTransform,
        Entity<FixturesComponent> grid) where T : IPhysShape
    {
        var gridTransform = _physics.GetPhysicsTransform(grid);

        while (enumerator.MoveNext(out var chunk))
        {
            foreach (var id in chunk.Fixtures)
            {
                var fixture = grid.Comp.Fixtures[id];

                for (var j = 0; j < fixture.Shape.ChildCount; j++)
                {
                    if (_manifolds.TestOverlap(shape, 0, fixture.Shape, j, shapeTransform, gridTransform))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    #region MapId

    public void FindGridsIntersecting<T>(MapId mapId, T shape, Transform transform,
        ref List<Entity<MapGridComponent>> grids, bool approx = IMapManager.Approximate, bool includeMap = IMapManager.IncludeMap) where T : IPhysShape
    {
        if (_mapSystem.TryGetMap(mapId, out var mapEnt))
            FindGridsIntersecting(mapEnt.Value, shape, transform, ref grids, approx, includeMap);
    }

    public void FindGridsIntersecting<T>(MapId mapId, T shape, Transform transform, GridCallback callback, bool approx = IMapManager.Approximate, bool includeMap = IMapManager.IncludeMap) where T : IPhysShape
    {
        if (_mapSystem.TryGetMap(mapId, out var mapEnt))
            FindGridsIntersecting(mapEnt.Value, shape, transform, callback, includeMap, approx);
    }

    public void FindGridsIntersecting(MapId mapId, Box2 worldAABB, GridCallback callback, bool approx = IMapManager.Approximate, bool includeMap = IMapManager.IncludeMap)
    {
        if (_mapSystem.TryGetMap(mapId, out var mapEnt))
            FindGridsIntersecting(mapEnt.Value, worldAABB, callback, approx, includeMap);
    }

    public void FindGridsIntersecting<TState>(MapId mapId, Box2 worldAABB, ref TState state, GridCallback<TState> callback, bool approx = IMapManager.Approximate, bool includeMap = IMapManager.IncludeMap)
    {
        if (_mapSystem.TryGetMap(mapId, out var map))
            FindGridsIntersecting(map.Value, worldAABB, ref state, callback, approx, includeMap);
    }

    public void FindGridsIntersecting(MapId mapId, Box2 worldAABB, ref List<Entity<MapGridComponent>> grids,
        bool approx = IMapManager.Approximate, bool includeMap = IMapManager.IncludeMap)
    {
        if (_mapSystem.TryGetMap(mapId, out var map))
            FindGridsIntersecting(map.Value, worldAABB, ref grids, approx, includeMap);
    }

    public void FindGridsIntersecting(MapId mapId, Box2Rotated worldBounds, GridCallback callback, bool approx = IMapManager.Approximate,
        bool includeMap = IMapManager.IncludeMap)
    {
        if (_mapSystem.TryGetMap(mapId, out var mapEnt))
            FindGridsIntersecting(mapEnt.Value, worldBounds, callback, approx, includeMap);
    }

    public void FindGridsIntersecting<TState>(MapId mapId, Box2Rotated worldBounds, ref TState state, GridCallback<TState> callback,
        bool approx = IMapManager.Approximate, bool includeMap = IMapManager.IncludeMap)
    {
        if (_mapSystem.TryGetMap(mapId, out var mapEnt))
            FindGridsIntersecting(mapEnt.Value, worldBounds, ref state, callback, approx, includeMap);
    }

    public void FindGridsIntersecting(MapId mapId, Box2Rotated worldBounds, ref List<Entity<MapGridComponent>> grids,
        bool approx = IMapManager.Approximate, bool includeMap = IMapManager.IncludeMap)
    {
        if (_mapSystem.TryGetMap(mapId, out var mapEnt))
            FindGridsIntersecting(mapEnt.Value, worldBounds, ref grids, approx, includeMap);
    }

    #endregion

    #region MapEnt

    public void FindGridsIntersecting<T>(
        EntityUid mapEnt,
        T shape,
        Transform transform,
        GridCallback callback,
        bool approx = IMapManager.Approximate,
        bool includeMap = IMapManager.IncludeMap) where T : IPhysShape
    {
        FindGridsIntersecting(mapEnt, shape, shape.ComputeAABB(transform, 0), transform, callback, approx, includeMap);
    }

    private void FindGridsIntersecting<T>(EntityUid mapEnt, T shape, Box2 worldAABB, Transform transform, GridCallback callback, bool approx = IMapManager.Approximate, bool includeMap = IMapManager.IncludeMap) where T : IPhysShape
    {
        // This is here so we don't double up on code.
        var state = callback;

        FindGridsIntersecting(mapEnt, shape, worldAABB, transform, ref state,
            static (EntityUid uid, MapGridComponent grid, ref GridCallback state) => state.Invoke(uid, grid),
            approx: approx, includeMap: includeMap);
    }

    public void FindGridsIntersecting<T, TState>(
        EntityUid mapEnt,
        T shape,
        Transform transform,
        ref TState state,
        GridCallback<TState> callback,
        bool approx = IMapManager.Approximate,
        bool includeMap = IMapManager.IncludeMap) where T : IPhysShape
    {
        FindGridsIntersecting(mapEnt, shape, shape.ComputeAABB(transform, 0), transform, ref state, callback, approx, includeMap);
    }

    private void FindGridsIntersecting<T, TState>(
        EntityUid mapEnt,
        T shape,
        Box2 worldAABB,
        Transform transform,
        ref TState state,
        GridCallback<TState> callback,
        bool approx = IMapManager.Approximate,
        bool includeMap = IMapManager.IncludeMap) where T : IPhysShape
    {
        if (!_gridTreeQuery.TryGetComponent(mapEnt, out var gridTree))
            return;

        if (includeMap && _gridQuery.TryGetComponent(mapEnt, out var mapGrid))
        {
            callback(mapEnt, mapGrid, ref state);
        }

        var gridState = new GridQueryState<T, TState>(
            callback,
            state,
            worldAABB,
            shape,
            transform,
            gridTree.Tree,
            _mapSystem,
            this,
            _transformSystem,
            approx);

        gridTree.Tree.Query(ref gridState, static (ref GridQueryState<T, TState> state, DynamicTree.Proxy proxy) =>
        {
            // Even for approximate we'll check if any chunks roughly overlap.
            var data = state.Tree.GetUserData(proxy);
            var gridInvMatrix = state.TransformSystem.GetInvWorldMatrix(data.Uid);
            var localAABB = gridInvMatrix.TransformBox(state.WorldAABB);

            var overlappingChunks = state.MapSystem.GetLocalMapChunks(data.Uid, data.Grid, localAABB);

            if (state.Approximate)
            {
                if (!overlappingChunks.MoveNext(out _))
                    return true;
            }
            else if (!state.MapManager.IsIntersecting(overlappingChunks, state.Shape, state.Transform, (data.Uid, data.Fixtures)))
            {
                return true;
            }

            var callbackState = state.State;
            var result = state.Callback(data.Uid, data.Grid, ref callbackState);
            state.State = callbackState;

            return result;
        }, worldAABB);

        // By-ref things
        state = gridState.State;
    }

    /// <summary>
    /// Returns true if any grids overlap the specified shapes.
    /// </summary>
    public void FindGridsIntersecting(EntityUid mapEnt, List<IPhysShape> shapes, Transform transform, ref List<Entity<MapGridComponent>> entities, bool approx = IMapManager.Approximate, bool includeMap = IMapManager.IncludeMap)
    {
        foreach (var shape in shapes)
        {
            FindGridsIntersecting(mapEnt, shape, shape.ComputeAABB(transform, 0), transform, ref entities);
        }
    }

    public void FindGridsIntersecting<T>(EntityUid mapEnt, T shape, Transform transform,
        ref List<Entity<MapGridComponent>> grids, bool approx = IMapManager.Approximate, bool includeMap = IMapManager.IncludeMap) where T : IPhysShape
    {
        FindGridsIntersecting(mapEnt, shape, shape.ComputeAABB(transform, 0), transform, ref grids, approx, includeMap);
    }

    public void FindGridsIntersecting<T>(EntityUid mapEnt, T shape, Box2 worldAABB, Transform transform,
        ref List<Entity<MapGridComponent>> grids, bool approx = IMapManager.Approximate, bool includeMap = IMapManager.IncludeMap) where T : IPhysShape
    {
        var state = grids;

        FindGridsIntersecting(mapEnt, shape, worldAABB, transform, ref state,
            static (EntityUid uid, MapGridComponent grid, ref List<Entity<MapGridComponent>> list) =>
            {
                list.Add((uid, grid));
                return true;
            }, approx, includeMap);
    }

    public void FindGridsIntersecting(EntityUid mapEnt, Box2 worldAABB, GridCallback callback, bool approx = IMapManager.Approximate, bool includeMap = IMapManager.IncludeMap)
    {
        var polygon = new SlimPolygon(worldAABB);
        FindGridsIntersecting(mapEnt, polygon, worldAABB, Transform.Empty, callback, approx, includeMap);
    }

    public void FindGridsIntersecting<TState>(EntityUid mapEnt, Box2 worldAABB, ref TState state, GridCallback<TState> callback, bool approx = IMapManager.Approximate, bool includeMap = IMapManager.IncludeMap)
    {
        var polygon = new SlimPolygon(worldAABB);
        FindGridsIntersecting(mapEnt, polygon, worldAABB, Transform.Empty, ref state, callback, approx, includeMap);
    }

    public void FindGridsIntersecting(EntityUid mapEnt, Box2 worldAABB, ref List<Entity<MapGridComponent>> grids,
        bool approx = IMapManager.Approximate, bool includeMap = IMapManager.IncludeMap)
    {
        var polygon = new SlimPolygon(worldAABB);
        FindGridsIntersecting(mapEnt, polygon, worldAABB, Transform.Empty, ref grids, approx, includeMap);
    }

    public void FindGridsIntersecting(EntityUid mapEnt, Box2Rotated worldBounds, GridCallback callback, bool approx = IMapManager.Approximate,
        bool includeMap = IMapManager.IncludeMap)
    {
        var polygon = new SlimPolygon(worldBounds);
        FindGridsIntersecting(mapEnt, polygon, worldBounds.CalcBoundingBox(), Transform.Empty, callback, approx, includeMap);
    }

    public void FindGridsIntersecting<TState>(EntityUid mapEnt, Box2Rotated worldBounds, ref TState state, GridCallback<TState> callback,
        bool approx = IMapManager.Approximate, bool includeMap = IMapManager.IncludeMap)
    {
        var polygon = new SlimPolygon(worldBounds);
        FindGridsIntersecting(mapEnt, polygon, worldBounds.CalcBoundingBox(), Transform.Empty, ref state, callback, approx, includeMap);
    }

    public void FindGridsIntersecting(EntityUid mapEnt, Box2Rotated worldBounds, ref List<Entity<MapGridComponent>> grids,
        bool approx = IMapManager.Approximate, bool includeMap = IMapManager.IncludeMap)
    {
        var polygon = new SlimPolygon(worldBounds);
        FindGridsIntersecting(mapEnt, polygon, worldBounds.CalcBoundingBox(), Transform.Empty, ref grids, approx, includeMap);
    }

    #endregion

    #region TryFindGridAt

    public bool TryFindGridAt(
        EntityUid mapEnt,
        Vector2 worldPos,
        out EntityUid uid,
        [NotNullWhen(true)] out MapGridComponent? grid)
    {
        var rangeVec = new Vector2(0.2f, 0.2f);

        // Need to enlarge the AABB by at least the grid shrinkage size.
        var aabb = new Box2(worldPos - rangeVec, worldPos + rangeVec);

        uid = EntityUid.Invalid;
        grid = null;
        var state = (uid, grid, worldPos, _mapSystem, _transformSystem);

        FindGridsIntersecting(mapEnt, aabb, ref state, static (EntityUid iUid, MapGridComponent iGrid, ref (
            EntityUid uid,
            MapGridComponent? grid,
            Vector2 worldPos,
            SharedMapSystem mapSystem,
            SharedTransformSystem xformSystem) tuple) =>
        {
            // Turn the worldPos into a localPos and work out the relevant chunk we need to check
            // This is much faster than iterating over every chunk individually.
            // (though now we need some extra calcs up front).

            // Doesn't use WorldBounds because it's just an AABB.
            var matrix = tuple.xformSystem.GetInvWorldMatrix(iUid);
            var localPos = Vector2.Transform(tuple.worldPos, matrix);

            // NOTE:
            // If you change this to use fixtures instead (i.e. if you want half-tiles) then you need to make sure
            // you account for the fact that fixtures are shrunk slightly!
            var chunkIndices = SharedMapSystem.GetChunkIndices(localPos, iGrid.ChunkSize);

            if (!iGrid.Chunks.TryGetValue(chunkIndices, out var chunk))
                return true;

            var chunkRelative = SharedMapSystem.GetChunkRelative(localPos, iGrid.ChunkSize);
            var chunkTile = chunk.GetTile(chunkRelative);

            if (chunkTile.IsEmpty)
                return true;

            tuple.uid = iUid;
            tuple.grid = iGrid;
            return false;
        }, approx: true, includeMap: false);

        if (state.grid == null && _gridQuery.TryGetComponent(mapEnt, out var mapGrid))
        {
            uid = mapEnt;
            grid = mapGrid;
            return true;
        }

        uid = state.uid;
        grid = state.grid;
        return grid != null;
    }

    /// <summary>
    /// Attempts to find the map grid under the map location.
    /// </summary>
    public bool TryFindGridAt(MapId mapId, Vector2 worldPos, out EntityUid uid, [NotNullWhen(true)] out MapGridComponent? grid)
    {
        if (_mapSystem.TryGetMap(mapId, out var map))
            return TryFindGridAt(map.Value, worldPos, out uid, out grid);

        uid = default;
        grid = null;
        return false;
    }

    /// <summary>
    /// Attempts to find the map grid under the map location.
    /// </summary>
    public bool TryFindGridAt(MapCoordinates mapCoordinates, out EntityUid uid, [NotNullWhen(true)] out MapGridComponent? grid)
    {
        return TryFindGridAt(mapCoordinates.MapId, mapCoordinates.Position, out uid, out grid);
    }

    #endregion

    private record struct GridQueryState<T, TState>(
        GridCallback<TState> Callback,
        TState State,
        Box2 WorldAABB,
        T Shape,
        Transform Transform,
        B2DynamicTree<(EntityUid Uid, FixturesComponent Fixtures, MapGridComponent Grid)> Tree,
        SharedMapSystem MapSystem,
        MapManager MapManager,
        SharedTransformSystem TransformSystem,
        bool Approximate);
}
