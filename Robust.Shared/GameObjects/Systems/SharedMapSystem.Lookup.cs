using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Map.Enumerators;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Shapes;
using Transform = Robust.Shared.Physics.Transform;

namespace Robust.Shared.GameObjects;

public abstract partial class SharedMapSystem
{
    #region TryFindGridAt

    public bool TryFindGridAt(EntityUid mapEnt, Vector2 worldPos, out EntityUid uid, [NotNullWhen(true)] out MapGridComponent? grid)
    {
        var rangeVec = new Vector2(0.2f, 0.2f);

        // Need to enlarge the AABB by at least the grid shrinkage size.
        var aabb = new Box2(worldPos - rangeVec, worldPos + rangeVec);

        uid = EntityUid.Invalid;
        grid = null;
        var state = (uid, grid, worldPos, this, _transform);

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

    public bool TryFindGridAt(MapId mapId, Vector2 worldPos, out EntityUid uid, [NotNullWhen(true)] out MapGridComponent? grid)
    {
        if (TryGetMap(mapId, out var map))
            return TryFindGridAt(map.Value, worldPos, out uid, out grid);

        uid = default;
        grid = null;
        return false;
    }

    public bool TryFindGridAt(MapCoordinates mapCoordinates, out EntityUid uid, [NotNullWhen(true)] out MapGridComponent? grid)
    {
        return TryFindGridAt(mapCoordinates.MapId, mapCoordinates.Position, out uid, out grid);
    }

    #endregion

    #region MapId

    public void FindGridsIntersecting<T>(
        MapId mapId,
        T shape,
        Transform transform,
        ref List<Entity<MapGridComponent>> grids,
        bool approx = IMapManager.Approximate,
        bool includeMap = IMapManager.IncludeMap) where T : IPhysShape
    {
        if (TryGetMap(mapId, out var mapEnt))
            FindGridsIntersecting(mapEnt.Value, shape, transform, ref grids, approx: approx, includeMap: includeMap);
    }

    public void FindGridsIntersecting<T>(
        MapId mapId,
        T shape,
        Transform transform,
        GridCallback callback,
        bool approx = IMapManager.Approximate,
        bool includeMap = IMapManager.IncludeMap) where T : IPhysShape
    {
        if (TryGetMap(mapId, out var mapEnt))
            FindGridsIntersecting(mapEnt.Value, shape, transform, callback, approx: approx, includeMap: includeMap);
    }

    public void FindGridsIntersecting(
        MapId mapId,
        Box2 worldAABB,
        GridCallback callback,
        bool approx = IMapManager.Approximate,
        bool includeMap = IMapManager.IncludeMap)
    {
        if (TryGetMap(mapId, out var mapEnt))
            FindGridsIntersecting(mapEnt.Value, worldAABB, callback, approx: approx, includeMap: includeMap);
    }

    public void FindGridsIntersecting<TState>(
        MapId mapId,
        Box2 worldAABB,
        ref TState state,
        GridCallback<TState> callback,
        bool approx = IMapManager.Approximate,
        bool includeMap = IMapManager.IncludeMap)
    {
        if (TryGetMap(mapId, out var map))
            FindGridsIntersecting(map.Value, worldAABB, ref state, callback, approx: approx, includeMap: includeMap);
    }

    public void FindGridsIntersecting(
        MapId mapId,
        Box2 worldAABB,
        ref List<Entity<MapGridComponent>> grids,
        bool approx = IMapManager.Approximate,
        bool includeMap = IMapManager.IncludeMap)
    {
        if (TryGetMap(mapId, out var map))
            FindGridsIntersecting(map.Value, worldAABB, ref grids, approx: approx, includeMap: includeMap);
    }

    public void FindGridsIntersecting(
        MapId mapId,
        Box2Rotated worldBounds,
        GridCallback callback,
        bool approx = IMapManager.Approximate,
        bool includeMap = IMapManager.IncludeMap)
    {
        if (TryGetMap(mapId, out var mapEnt))
            FindGridsIntersecting(mapEnt.Value, worldBounds, callback, approx: approx, includeMap: includeMap);
    }

    public void FindGridsIntersecting<TState>(
        MapId mapId,
        Box2Rotated worldBounds,
        ref TState state,
        GridCallback<TState> callback,
        bool approx = IMapManager.Approximate,
        bool includeMap = IMapManager.IncludeMap)
    {
        if (TryGetMap(mapId, out var mapEnt))
            FindGridsIntersecting(mapEnt.Value, worldBounds, ref state, callback, approx: approx, includeMap: includeMap);
    }

    public void FindGridsIntersecting(
        MapId mapId,
        Box2Rotated worldBounds,
        ref List<Entity<MapGridComponent>> grids,
        bool approx = IMapManager.Approximate,
        bool includeMap = IMapManager.IncludeMap)
    {
        if (TryGetMap(mapId, out var mapEnt))
            FindGridsIntersecting(mapEnt.Value, worldBounds, ref grids, approx: approx, includeMap: includeMap);
    }

    #endregion

    #region EntityUid

    public void FindGridsIntersecting<TShape>(
        EntityUid mapEnt,
        TShape shape,
        Transform transform,
        GridCallback callback,
        bool approx = false,
        bool includeMap = true) where TShape : IPhysShape
    {
        FindGridsIntersecting(mapEnt, shape, shape.ComputeAABB(transform, 0), transform, callback, approx: approx, includeMap: includeMap);
    }

    public void FindGridsIntersecting<TShape, TState>(
        EntityUid mapEnt,
        TShape shape,
        Transform transform,
        ref TState state,
        GridCallback<TState> callback,
        bool approx = false,
        bool includeMap = true) where TShape : IPhysShape
    {
        FindGridsIntersecting(mapEnt, shape, shape.ComputeAABB(transform, 0), transform, ref state, callback, approx: approx, includeMap: includeMap);
    }

    public void FindGridsIntersecting(
        EntityUid mapEnt,
        List<IPhysShape> shapes,
        Transform transform,
        ref List<Entity<MapGridComponent>> entities,
        bool approx = false,
        bool includeMap = true)
    {
        foreach (var shape in shapes)
        {
            FindGridsIntersecting(mapEnt, shape, transform, ref entities, approx: approx, includeMap: includeMap);
        }
    }

    public void FindGridsIntersecting<TShape>(
        EntityUid mapEnt,
        TShape shape,
        Transform transform,
        ref List<Entity<MapGridComponent>> grids,
        bool approx = false,
        bool includeMap = true) where TShape : IPhysShape
    {
        FindGridsIntersecting(mapEnt, shape, shape.ComputeAABB(transform, 0), transform, ref grids, approx: approx, includeMap: includeMap);
    }

    public void FindGridsIntersecting(
        EntityUid mapEnt,
        Box2 worldAABB,
        GridCallback callback,
        bool approx = false,
        bool includeMap = true)
    {
        var shape = new SlimPolygon(worldAABB);
        FindGridsIntersecting(mapEnt, shape, worldAABB, Robust.Shared.Physics.Transform.Empty, callback, approx: approx, includeMap: includeMap);
    }

    public void FindGridsIntersecting<TState>(
        EntityUid mapEnt,
        Box2 worldAABB,
        ref TState state,
        GridCallback<TState> callback,
        bool approx = false,
        bool includeMap = true)
    {
        var shape = new SlimPolygon(worldAABB);
        FindGridsIntersecting(mapEnt, shape, worldAABB, Robust.Shared.Physics.Transform.Empty, ref state, callback, approx: approx, includeMap: includeMap);
    }

    public void FindGridsIntersecting(
        EntityUid mapEnt,
        Box2 worldAABB,
        ref List<Entity<MapGridComponent>> grids,
        bool approx = false,
        bool includeMap = true)
    {
        var shape = new SlimPolygon(worldAABB);
        FindGridsIntersecting(mapEnt, shape, worldAABB, Robust.Shared.Physics.Transform.Empty, ref grids, approx: approx, includeMap: includeMap);
    }

    public void FindGridsIntersecting(
        EntityUid mapEnt,
        Box2Rotated worldBounds,
        GridCallback callback,
        bool approx = false,
        bool includeMap = true)
    {
        var shape = new SlimPolygon(worldBounds);
        FindGridsIntersecting(mapEnt, shape, Robust.Shared.Physics.Transform.Empty, callback, approx: approx, includeMap: includeMap);
    }

    public void FindGridsIntersecting<TState>(
        EntityUid mapEnt,
        Box2Rotated worldBounds,
        ref TState state,
        GridCallback<TState> callback,
        bool approx = false,
        bool includeMap = true)
    {
        var shape = new SlimPolygon(worldBounds);
        FindGridsIntersecting(mapEnt, shape, Robust.Shared.Physics.Transform.Empty, ref state, callback, approx: approx, includeMap: includeMap);
    }

    public void FindGridsIntersecting(
        EntityUid mapEnt,
        Box2Rotated worldBounds,
        ref List<Entity<MapGridComponent>> grids,
        bool approx = false,
        bool includeMap = true)
    {
        var shape = new SlimPolygon(worldBounds);
        FindGridsIntersecting(mapEnt, shape, Robust.Shared.Physics.Transform.Empty, ref grids, approx: approx, includeMap: includeMap);
    }

    #endregion

    public IEnumerable<Entity<MapGridComponent>> GetAllGrids(MapId mapId)
    {
        throw new System.NotImplementedException();
    }

    public IEnumerable<MapGridComponent> GetAllMapGrids(MapId mapId)
    {
        throw new System.NotImplementedException();
    }

    public void FindGridsIntersecting<TShape>(
        EntityUid mapEnt,
        TShape shape,
        Box2 worldAABB,
        Transform transform,
        ref List<Entity<MapGridComponent>> grids,
        bool approx,
        bool includeMap) where TShape : IPhysShape
    {
        var state = grids;
        FindGridsIntersecting(mapEnt, shape, worldAABB, transform, ref state,
            static (EntityUid uid, MapGridComponent grid, ref List<Entity<MapGridComponent>> state) =>
            {
                state.Add((uid, grid));
                return true;
            },
            approx, includeMap
        );
    }

    private void FindGridsIntersecting<TShape>(
        EntityUid mapEnt,
        TShape shape,
        Box2 worldAABB,
        Transform transform,
        GridCallback callback,
        bool approx,
        bool includeMap) where TShape : IPhysShape
    {
        var state = callback;
        FindGridsIntersecting(mapEnt, shape, worldAABB, transform, ref state,
            static (EntityUid uid, MapGridComponent grid, ref GridCallback state) => state.Invoke(uid, grid),
            approx, includeMap
        );
    }

    private void FindGridsIntersecting<TShape, TState>(
        EntityUid mapEnt,
        TShape shape,
        Box2 worldAABB,
        Transform transform,
        ref TState state,
        GridCallback<TState> callback,
        bool approx,
        bool includeMap) where TShape : IPhysShape
    {
        if (!_gridTreeQuery.TryGetComponent(mapEnt, out var gridTree))
            return;

        if (includeMap && _gridQuery.TryGetComponent(mapEnt, out var mapGrid))
        {
            callback(mapEnt, mapGrid, ref state);
        }

        var gridState = new GridQueryState<TShape, TState>(
            callback,
            state,
            worldAABB,
            shape,
            transform,
            gridTree.Tree,
            this,
            _transform,
            approx);

        gridTree.Tree.Query(ref gridState, static (ref GridQueryState<TShape, TState> state, DynamicTree.Proxy proxy) =>
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
            else if (!state.MapSystem.IsIntersecting(overlappingChunks, state.Shape, state.Transform, (data.Uid, data.Fixtures)))
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

    private bool IsIntersecting<TShape>(
        ChunkEnumerator enumerator,
        TShape shape,
        Transform shapeTransform,
        Entity<FixturesComponent> grid) where TShape : IPhysShape
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

    private record struct GridQueryState<TShape, TState>(
        GridCallback<TState> Callback,
        TState State,
        Box2 WorldAABB,
        TShape Shape,
        Transform Transform,
        B2DynamicTree<(EntityUid Uid, FixturesComponent Fixtures, MapGridComponent Grid)> Tree,
        SharedMapSystem MapSystem,
        SharedTransformSystem TransformSystem,
        bool Approximate
    ) where TShape : IPhysShape;
}
