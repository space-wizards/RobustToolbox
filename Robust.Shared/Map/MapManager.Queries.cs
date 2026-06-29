using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;

namespace Robust.Shared.Map;

internal partial class MapManager
{
    #region MapId [Obsolete]

    [Obsolete("use SharedMapSystem")]
    public void FindGridsIntersecting<T>(MapId mapId, T shape, Transform transform,
        ref List<Entity<MapGridComponent>> grids, bool approx = IMapManager.Approximate, bool includeMap = IMapManager.IncludeMap) where T : IPhysShape
    {
        MapSystem.FindGridsIntersecting(mapId, shape, transform, ref grids, approx: approx, includeMap: includeMap);
    }

    [Obsolete("use SharedMapSystem")]
    public void FindGridsIntersecting<T>(MapId mapId, T shape, Transform transform, GridCallback callback, bool approx = IMapManager.Approximate, bool includeMap = IMapManager.IncludeMap) where T : IPhysShape
    {
        MapSystem.FindGridsIntersecting(mapId, shape, transform, callback, approx: approx, includeMap: includeMap);
    }

    [Obsolete("use SharedMapSystem")]
    public void FindGridsIntersecting(MapId mapId, Box2 worldAABB, GridCallback callback, bool approx = IMapManager.Approximate, bool includeMap = IMapManager.IncludeMap)
    {
        MapSystem.FindGridsIntersecting(mapId, worldAABB, callback, approx: approx, includeMap: includeMap);
    }

    [Obsolete("use SharedMapSystem")]
    public void FindGridsIntersecting<TState>(MapId mapId, Box2 worldAABB, ref TState state, GridCallback<TState> callback, bool approx = IMapManager.Approximate, bool includeMap = IMapManager.IncludeMap)
    {
        MapSystem.FindGridsIntersecting(mapId, worldAABB, ref state, callback, approx: approx, includeMap: includeMap);
    }

    [Obsolete("use SharedMapSystem")]
    public void FindGridsIntersecting(MapId mapId, Box2 worldAABB, ref List<Entity<MapGridComponent>> grids,
        bool approx = IMapManager.Approximate, bool includeMap = IMapManager.IncludeMap)
    {
        MapSystem.FindGridsIntersecting(mapId, worldAABB, ref grids, approx: approx, includeMap: includeMap);
    }

    [Obsolete("use SharedMapSystem")]
    public void FindGridsIntersecting(MapId mapId, Box2Rotated worldBounds, GridCallback callback, bool approx = IMapManager.Approximate,
        bool includeMap = IMapManager.IncludeMap)
    {
        MapSystem.FindGridsIntersecting(mapId, worldBounds, callback, approx: approx, includeMap: includeMap);
    }

    [Obsolete("use SharedMapSystem")]
    public void FindGridsIntersecting<TState>(MapId mapId, Box2Rotated worldBounds, ref TState state, GridCallback<TState> callback,
        bool approx = IMapManager.Approximate, bool includeMap = IMapManager.IncludeMap)
    {
        MapSystem.FindGridsIntersecting(mapId, worldBounds, ref state, callback, approx: approx, includeMap: includeMap);
    }

    [Obsolete("use SharedMapSystem")]
    public void FindGridsIntersecting(MapId mapId, Box2Rotated worldBounds, ref List<Entity<MapGridComponent>> grids,
        bool approx = IMapManager.Approximate, bool includeMap = IMapManager.IncludeMap)
    {
        MapSystem.FindGridsIntersecting(mapId, worldBounds, ref grids, approx: approx, includeMap: includeMap);
    }

    #endregion

    #region MapEnt [Obsolete]

    [Obsolete("use SharedMapSystem")]
    public void FindGridsIntersecting<T>(
        EntityUid mapEnt,
        T shape,
        Transform transform,
        GridCallback callback,
        bool approx = IMapManager.Approximate,
        bool includeMap = IMapManager.IncludeMap) where T : IPhysShape
    {
        MapSystem.FindGridsIntersecting(mapEnt, shape, transform, callback, approx: approx, includeMap: includeMap);
    }

    [Obsolete("use SharedMapSystem")]
    public void FindGridsIntersecting<T, TState>(
        EntityUid mapEnt,
        T shape,
        Transform transform,
        ref TState state,
        GridCallback<TState> callback,
        bool approx = IMapManager.Approximate,
        bool includeMap = IMapManager.IncludeMap) where T : IPhysShape
    {
        MapSystem.FindGridsIntersecting(mapEnt, shape, transform, ref state, callback, approx: approx, includeMap: includeMap);
    }

    /// <summary>
    /// Returns true if any grids overlap the specified shapes.
    /// </summary>
    [Obsolete("use SharedMapSystem")]
    public void FindGridsIntersecting(EntityUid mapEnt, List<IPhysShape> shapes, Transform transform, ref List<Entity<MapGridComponent>> entities, bool approx = IMapManager.Approximate, bool includeMap = IMapManager.IncludeMap)
    {
        MapSystem.FindGridsIntersecting(mapEnt, shapes, transform, ref entities, approx: approx, includeMap: includeMap);
    }

    [Obsolete("use SharedMapSystem")]
    public void FindGridsIntersecting<T>(EntityUid mapEnt, T shape, Transform transform,
        ref List<Entity<MapGridComponent>> grids, bool approx = IMapManager.Approximate, bool includeMap = IMapManager.IncludeMap) where T : IPhysShape
    {
        MapSystem.FindGridsIntersecting(mapEnt, shape, transform, ref grids, approx: approx, includeMap: includeMap);
    }

    [Obsolete("use SharedMapSystem")]
    public void FindGridsIntersecting<T>(EntityUid mapEnt, T shape, Box2 worldAABB, Transform transform,
        ref List<Entity<MapGridComponent>> grids, bool approx = IMapManager.Approximate, bool includeMap = IMapManager.IncludeMap) where T : IPhysShape
    {
        MapSystem.FindGridsIntersecting(mapEnt, shape, worldAABB, transform, ref grids, approx: approx, includeMap: includeMap);
    }

    [Obsolete("use SharedMapSystem")]
    public void FindGridsIntersecting(EntityUid mapEnt, Box2 worldAABB, GridCallback callback, bool approx = IMapManager.Approximate, bool includeMap = IMapManager.IncludeMap)
    {
        MapSystem.FindGridsIntersecting(mapEnt, worldAABB, callback, approx: approx, includeMap: includeMap);
    }

    [Obsolete("use SharedMapSystem")]
    public void FindGridsIntersecting<TState>(EntityUid mapEnt, Box2 worldAABB, ref TState state, GridCallback<TState> callback, bool approx = IMapManager.Approximate, bool includeMap = IMapManager.IncludeMap)
    {
        MapSystem.FindGridsIntersecting(mapEnt, worldAABB, ref state, callback, approx: approx, includeMap: includeMap);
    }

    [Obsolete("use SharedMapSystem")]
    public void FindGridsIntersecting(EntityUid mapEnt, Box2 worldAABB, ref List<Entity<MapGridComponent>> grids,
        bool approx = IMapManager.Approximate, bool includeMap = IMapManager.IncludeMap)
    {
        MapSystem.FindGridsIntersecting(mapEnt, worldAABB, ref grids, approx: approx, includeMap: includeMap);
    }

    [Obsolete("use SharedMapSystem")]
    public void FindGridsIntersecting(EntityUid mapEnt, Box2Rotated worldBounds, GridCallback callback, bool approx = IMapManager.Approximate,
        bool includeMap = IMapManager.IncludeMap)
    {
        MapSystem.FindGridsIntersecting(mapEnt, worldBounds, callback, approx: approx, includeMap: includeMap);
    }

    [Obsolete("use SharedMapSystem")]
    public void FindGridsIntersecting<TState>(EntityUid mapEnt, Box2Rotated worldBounds, ref TState state, GridCallback<TState> callback,
        bool approx = IMapManager.Approximate, bool includeMap = IMapManager.IncludeMap)
    {
        MapSystem.FindGridsIntersecting(mapEnt, worldBounds, ref state, callback, approx: approx, includeMap: includeMap);
    }

    [Obsolete("use SharedMapSystem")]
    public void FindGridsIntersecting(EntityUid mapEnt, Box2Rotated worldBounds, ref List<Entity<MapGridComponent>> grids,
        bool approx = IMapManager.Approximate, bool includeMap = IMapManager.IncludeMap)
    {
        MapSystem.FindGridsIntersecting(mapEnt, worldBounds, ref grids, approx: approx, includeMap: includeMap);
    }

    #endregion

    #region TryFindGridAt

    [Obsolete("use SharedMapSystem")]
    public bool TryFindGridAt(
        EntityUid mapEnt,
        Vector2 worldPos,
        out EntityUid uid,
        [NotNullWhen(true)] out MapGridComponent? grid)
    {
        return MapSystem.TryFindGridAt(mapEnt, worldPos, out uid, out grid);
    }

    /// <summary>
    /// Attempts to find the map grid under the map location.
    /// </summary>
    [Obsolete("use SharedMapSystem")]
    public bool TryFindGridAt(MapId mapId, Vector2 worldPos, out EntityUid uid, [NotNullWhen(true)] out MapGridComponent? grid)
    {
        return MapSystem.TryFindGridAt(mapId, worldPos, out uid, out grid);
    }

    /// <summary>
    /// Attempts to find the map grid under the map location.
    /// </summary>
    [Obsolete("use SharedMapSystem")]
    public bool TryFindGridAt(MapCoordinates mapCoordinates, out EntityUid uid, [NotNullWhen(true)] out MapGridComponent? grid)
    {
        return MapSystem.TryFindGridAt(mapCoordinates, out uid, out grid);
    }

    #endregion
}
