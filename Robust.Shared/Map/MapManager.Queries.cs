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

namespace Robust.Shared.Map;

internal partial class MapManager
{
    private bool IsIntersecting(
        ChunkEnumerator enumerator,
        IPhysShape shape,
        Transform shapeTransform,
        EntityUid gridUid)
    {
        var gridTransform = _physics.GetPhysicsTransform(gridUid);

        while (enumerator.MoveNext(out var chunk))
        {
            foreach (var fixture in chunk.Fixtures.Values)
            {
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

    public void FindGridsIntersecting(MapId mapId, IPhysShape shape, Transform transform,
        ref List<Entity<MapGridComponent>> grids, bool approx = IMapManager.Approximate, bool includeMap = IMapManager.IncludeMap)
    {
        if (_mapEntities.TryGetValue(mapId, out var mapEnt))
            FindGridsIntersecting(mapEnt, shape, transform, ref grids, approx, includeMap);
    }

    public void FindGridsIntersecting(MapId mapId, PolygonShape shape, Transform transform, GridCallback callback, bool approx = IMapManager.Approximate, bool includeMap = IMapManager.IncludeMap)
    {
        if (_mapEntities.TryGetValue(mapId, out var mapEnt))
            FindGridsIntersecting(mapEnt, shape, transform, callback, includeMap, approx);
    }

    public void FindGridsIntersecting(MapId mapId, Box2 worldAABB, GridCallback callback, bool approx = IMapManager.Approximate, bool includeMap = IMapManager.IncludeMap)
    {
        if (_mapEntities.TryGetValue(mapId, out var mapEnt))
            FindGridsIntersecting(mapEnt, worldAABB, callback, approx, includeMap);
    }

    public void FindGridsIntersecting<TState>(MapId mapId, Box2 worldAABB, ref TState state, GridCallback<TState> callback, bool approx = IMapManager.Approximate, bool includeMap = IMapManager.IncludeMap)
    {
        if (_mapEntities.TryGetValue(mapId, out var map))
            FindGridsIntersecting(map, worldAABB, ref state, callback, approx, includeMap);
    }

    public void FindGridsIntersecting(MapId mapId, Box2 worldAABB, ref List<Entity<MapGridComponent>> grids,
        bool approx = IMapManager.Approximate, bool includeMap = IMapManager.IncludeMap)
    {
        if (_mapEntities.TryGetValue(mapId, out var map))
            FindGridsIntersecting(map, worldAABB, ref grids, approx, includeMap);
    }

    public void FindGridsIntersecting(MapId mapId, Box2Rotated worldBounds, GridCallback callback, bool approx = IMapManager.Approximate,
        bool includeMap = IMapManager.IncludeMap)
    {
        if (_mapEntities.TryGetValue(mapId, out var mapEnt))
            FindGridsIntersecting(mapEnt, worldBounds, callback, approx, includeMap);
    }

    public void FindGridsIntersecting<TState>(MapId mapId, Box2Rotated worldBounds, ref TState state, GridCallback<TState> callback,
        bool approx = IMapManager.Approximate, bool includeMap = IMapManager.IncludeMap)
    {
        if (_mapEntities.TryGetValue(mapId, out var mapEnt))
            FindGridsIntersecting(mapEnt, worldBounds, ref state, callback, approx, includeMap);
    }

    public void FindGridsIntersecting(MapId mapId, Box2Rotated worldBounds, ref List<Entity<MapGridComponent>> grids,
        bool approx = IMapManager.Approximate, bool includeMap = IMapManager.IncludeMap)
    {
        if (_mapEntities.TryGetValue(mapId, out var mapEnt))
            FindGridsIntersecting(mapEnt, worldBounds, ref grids, approx, includeMap);
    }

    #endregion

    #region MapEnt

    public void FindGridsIntersecting(EntityUid mapEnt, PolygonShape shape, Transform transform, GridCallback callback, bool approx = IMapManager.Approximate, bool includeMap = IMapManager.IncludeMap)
    {
        if (!_gridTreeQuery.TryGetComponent(mapEnt, out var gridTree))
            return;

        if (includeMap && _gridQuery.TryGetComponent(mapEnt, out var mapGrid))
        {
            callback(mapEnt, mapGrid);
        }

        var worldAABB = shape.ComputeAABB(transform, 0);

        var gridState = new GridQueryState(
            callback,
            worldAABB,
            shape,
            transform,
            gridTree.Tree,
            _mapSystem,
            this,
            _transformSystem,
            approx);


        gridTree.Tree.Query(ref gridState, static (ref GridQueryState state, DynamicTree.Proxy proxy) =>
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
            else if (!state.MapManager.IsIntersecting(overlappingChunks, state.Shape, state.Transform, data.Uid))
            {
                return true;
            }

            state.Callback(data.Uid, data.Grid);

            return true;
        }, worldAABB);
    }

    public void FindGridsIntersecting<TState>(EntityUid mapEnt, IPhysShape shape, Transform transform,
        ref TState state, GridCallback<TState> callback, bool approx = IMapManager.Approximate, bool includeMap = IMapManager.IncludeMap)
    {
        if (!_gridTreeQuery.TryGetComponent(mapEnt, out var gridTree))
            return;

        if (includeMap && _gridQuery.TryGetComponent(mapEnt, out var mapGrid))
        {
            callback(mapEnt, mapGrid, ref state);
        }

        var worldAABB = shape.ComputeAABB(transform, 0);

        var gridState = new GridQueryState<TState>(
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


        gridTree.Tree.Query(ref gridState, static (ref GridQueryState<TState> state, DynamicTree.Proxy proxy) =>
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
            else if (!state.MapManager.IsIntersecting(overlappingChunks, state.Shape, state.Transform, data.Uid))
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
            FindGridsIntersecting(mapEnt, shape, transform, ref entities);
        }
    }

    public void FindGridsIntersecting(EntityUid mapEnt, IPhysShape shape, Transform transform,
        ref List<Entity<MapGridComponent>> grids, bool approx = IMapManager.Approximate, bool includeMap = IMapManager.IncludeMap)
    {
        var state = grids;

        FindGridsIntersecting(mapEnt, shape, transform, ref state,
            static (EntityUid uid, MapGridComponent grid, ref List<Entity<MapGridComponent>> list) =>
            {
                list.Add((uid, grid));
                return true;
            }, approx, includeMap);
    }

    public void FindGridsIntersecting(EntityUid mapEnt, Box2 worldAABB, GridCallback callback, bool approx = IMapManager.Approximate, bool includeMap = IMapManager.IncludeMap)
    {
        var shape = new PolygonShape();
        shape.SetAsBox(worldAABB);
        FindGridsIntersecting(mapEnt, shape, Transform.Empty, callback, approx, includeMap);
    }

    public void FindGridsIntersecting<TState>(EntityUid mapEnt, Box2 worldAABB, ref TState state, GridCallback<TState> callback, bool approx = IMapManager.Approximate, bool includeMap = IMapManager.IncludeMap)
    {
        var shape = new PolygonShape();
        shape.SetAsBox(worldAABB);
        FindGridsIntersecting(mapEnt, shape, Transform.Empty, ref state, callback, approx, includeMap);
    }

    public void FindGridsIntersecting(EntityUid mapEnt, Box2 worldAABB, ref List<Entity<MapGridComponent>> grids,
        bool approx = IMapManager.Approximate, bool includeMap = IMapManager.IncludeMap)
    {
        var shape = new PolygonShape();
        shape.SetAsBox(worldAABB);
        FindGridsIntersecting(mapEnt, shape, Transform.Empty, ref grids, approx, includeMap);
    }

    public void FindGridsIntersecting(EntityUid mapEnt, Box2Rotated worldBounds, GridCallback callback, bool approx = IMapManager.Approximate,
        bool includeMap = IMapManager.IncludeMap)
    {
        var shape = new PolygonShape();
        shape.Set(worldBounds);

        FindGridsIntersecting(mapEnt, shape, Transform.Empty, callback, approx, includeMap);
    }

    public void FindGridsIntersecting<TState>(EntityUid mapEnt, Box2Rotated worldBounds, ref TState state, GridCallback<TState> callback,
        bool approx = IMapManager.Approximate, bool includeMap = IMapManager.IncludeMap)
    {
        var shape = new PolygonShape();
        shape.Set(worldBounds);

        FindGridsIntersecting(mapEnt, shape, Transform.Empty, ref state, callback, approx, includeMap);
    }

    public void FindGridsIntersecting(EntityUid mapEnt, Box2Rotated worldBounds, ref List<Entity<MapGridComponent>> grids,
        bool approx = IMapManager.Approximate, bool includeMap = IMapManager.IncludeMap)
    {
        var shape = new PolygonShape();
        shape.Set(worldBounds);

        FindGridsIntersecting(mapEnt, shape, Transform.Empty, ref grids, approx, includeMap);
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
            var localPos = matrix.Transform(tuple.worldPos);

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
        if (_mapEntities.TryGetValue(mapId, out var map))
            return TryFindGridAt(map, worldPos, out uid, out grid);

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

    private readonly record struct GridQueryState(
        GridCallback Callback,
        Box2 WorldAABB,
        IPhysShape Shape,
        Transform Transform,
        B2DynamicTree<(EntityUid Uid, MapGridComponent Grid)> Tree,
        SharedMapSystem MapSystem,
        MapManager MapManager,
        SharedTransformSystem TransformSystem,
        bool Approximate);

    private record struct GridQueryState<TState>(
        GridCallback<TState> Callback,
        TState State,
        Box2 WorldAABB,
        IPhysShape Shape,
        Transform Transform,
        B2DynamicTree<(EntityUid Uid, MapGridComponent Grid)> Tree,
        SharedMapSystem MapSystem,
        MapManager MapManager,
        SharedTransformSystem TransformSystem,
        bool Approximate);
}
