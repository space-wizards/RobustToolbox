using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Robust.Shared.GameObjects;

public sealed partial class EntityLookupSystem
{
    #region Box2

    public bool AnyEntitiesIntersecting(MapId mapId, Box2 worldAABB, LookupFlags flags = DefaultFlags)
    {
        if (mapId == MapId.Nullspace) return false;

        var lookupQuery = GetEntityQuery<EntityLookupComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();

        var found = false;
        EntityLookupComponent lookup;
        Box2 localAABB;

        foreach (var grid in _mapManager.FindGridsIntersecting(mapId, worldAABB))
        {
            lookup = lookupQuery.GetComponent(grid.GridEntityId);
            localAABB = xformQuery.GetComponent(grid.GridEntityId).InvWorldMatrix.TransformBox(worldAABB);

            lookup.Tree._b2Tree.Query(ref found, static (ref bool state, DynamicTree.Proxy _) =>
            {
                state = true;
                return false;
            }, localAABB);

            if (found)
                return true;
        }

        var mapUid = _mapManager.GetMapEntityId(mapId);
        lookup = lookupQuery.GetComponent(mapUid);
        localAABB = xformQuery.GetComponent(mapUid).InvWorldMatrix.TransformBox(worldAABB);

        lookup.Tree._b2Tree.Query(ref found, static (ref bool state, DynamicTree.Proxy _) =>
        {
            state = true;
            return false;
        }, localAABB);

        return found;
    }

    public IEnumerable<EntityUid> GetEntitiesIntersecting(MapId mapId, Box2 worldAABB, LookupFlags flags = DefaultFlags)
    {
        throw new NotImplementedException();
    }

    #endregion

    #region Box2Rotated

    public bool AnyEntitiesIntersecting(MapId mapId, Box2Rotated worldBounds, LookupFlags flags = DefaultFlags)
    {
        var lookupQuery = GetEntityQuery<EntityLookupComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();

        foreach (var grid in _mapManager.FindGridsIntersecting(mapId, worldBounds))
        {
            var lookup = lookupQuery.GetComponent(grid.GridEntityId);
            var localAABB = xformQuery.GetComponent(grid.GridEntityId).InvWorldMatrix.TransformBox(worldBounds);
            var found = false;

            lookup.Tree._b2Tree.Query(ref found, static (ref bool b, DynamicTree.Proxy _) =>
            {
                b = true;
                return false;
            }, localAABB);

            if (found)
                return true;
        }

        return false;
    }

    public IEnumerable<EntityUid> GetEntitiesIntersecting(MapId mapId, Box2 worldBounds, LookupFlags flags = DefaultFlags)
    {
        throw new NotImplementedException();
    }

    #endregion

    #region Entity

    public bool AnyEntitiesIntersecting(EntityUid uid, LookupFlags flags = DefaultFlags)
    {
        throw new NotImplementedException();
    }

    #endregion

    #region EntityCoordinates

    public bool AnyEntitiesIntersecting(EntityCoordinates coordinates, LookupFlags flags = DefaultFlags)
    {
        if (!coordinates.IsValid(EntityManager)) return false;

        throw new NotImplementedException();
    }

    public bool AnyEntitiesIntersecting(EntityCoordinates coordinates, float range, LookupFlags flags = DefaultFlags)
    {
        throw new NotImplementedException();
    }

    #endregion

    #region MapCoordinates

    public bool AnyEntitiesIntersecting(MapCoordinates coordinates, LookupFlags flags = DefaultFlags)
    {
        if (coordinates.MapId == MapId.Nullspace) return false;

        throw new NotImplementedException();
    }

    public bool AnyEntitiesIntersecting(MapCoordinates coordinates, float range, LookupFlags flags = DefaultFlags)
    {
        throw new NotImplementedException();
    }

    #endregion

    #region Grid Methods

    /// <summary>
    /// Returns the entities intersecting any of the supplied tiles. Faster than doing each tile individually.
    /// </summary>
    /// <param name="gridId"></param>
    /// <param name="gridIndices"></param>
    /// <returns></returns>
    public IEnumerable<EntityUid> GetEntitiesIntersecting(GridId gridId, IEnumerable<Vector2i> gridIndices)
    {
        // Technically this doesn't consider anything overlapping from outside the grid but is this an issue?
        if (!_mapManager.TryGetGrid(gridId, out var grid)) return Enumerable.Empty<EntityUid>();

        var lookup = EntityManager.GetComponent<EntityLookupComponent>(grid.GridEntityId);
        var results = new HashSet<EntityUid>();
        var tileSize = grid.TileSize;

        // TODO: You can probably decompose the indices into larger areas if you take in a hashset instead.
        foreach (var index in gridIndices)
        {
            var aabb = GetLocalBounds(index, tileSize);

            lookup.Tree._b2Tree.FastQuery(ref aabb, (ref EntityUid data) =>
            {
                if (EntityManager.Deleted(data)) return;
                results.Add(data);
            });

            foreach (var ent in grid.GetAnchoredEntities(index))
            {
                if (EntityManager.Deleted(ent)) continue;
                results.Add(ent);
            }
        }

        return results;
    }

    public IEnumerable<EntityUid> GetEntitiesIntersecting(GridId gridId, Vector2i gridIndices)
    {
        // Technically this doesn't consider anything overlapping from outside the grid but is this an issue?
        if (!_mapManager.TryGetGrid(gridId, out var grid)) return Enumerable.Empty<EntityUid>();

        var lookup = EntityManager.GetComponent<EntityLookupComponent>(grid.GridEntityId);
        var tileSize = grid.TileSize;

        var aabb = GetLocalBounds(gridIndices, tileSize);
        var results = new HashSet<EntityUid>();

        lookup.Tree._b2Tree.FastQuery(ref aabb, (ref EntityUid data) =>
        {
            if (EntityManager.Deleted(data)) return;
            results.Add(data);
        });

        foreach (var ent in grid.GetAnchoredEntities(gridIndices))
        {
            if (EntityManager.Deleted(ent)) continue;
            results.Add(ent);
        }

        return results;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerable<EntityUid> GetEntitiesIntersecting(TileRef tileRef)
    {
        return GetEntitiesIntersecting(tileRef.GridIndex, tileRef.GridIndices);
    }

    #endregion

    #region Lookups

    /// <summary>
    /// Gets the relevant <see cref="EntityLookupComponent"/> that intersects the specified area.
    /// </summary>
    public IEnumerable<EntityLookupComponent> FindLookupsIntersecting(MapId mapId, Box2 worldAABB)
    {
        if (mapId == MapId.Nullspace) yield break;

        var lookupQuery = EntityManager.GetEntityQuery<EntityLookupComponent>();

        yield return lookupQuery.GetComponent(_mapManager.GetMapEntityId(mapId));

        foreach (var grid in _mapManager.FindGridsIntersecting(mapId, worldAABB))
        {
            yield return lookupQuery.GetComponent(grid.GridEntityId);
        }
    }

    /// <summary>
    /// Gets the relevant <see cref="EntityLookupComponent"/> that intersects the specified area.
    /// </summary>
    public IEnumerable<EntityLookupComponent> FindLookupsIntersecting(MapId mapId, Box2Rotated worldBounds)
    {
        if (mapId == MapId.Nullspace) yield break;

        var lookupQuery = EntityManager.GetEntityQuery<EntityLookupComponent>();

        yield return lookupQuery.GetComponent(_mapManager.GetMapEntityId(mapId));

        // Copy-paste with above but the query may differ slightly internally.
        foreach (var grid in _mapManager.FindGridsIntersecting(mapId, worldBounds))
        {
            yield return lookupQuery.GetComponent(grid.GridEntityId);
        }
    }

    #endregion

    #region Bounds

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Box2 GetLocalBounds(Vector2i gridIndices, ushort tileSize)
    {
        return new Box2(gridIndices * tileSize, (gridIndices + 1) * tileSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Box2 GetLocalBounds(TileRef tileRef, ushort tileSize)
    {
        return GetLocalBounds(tileRef.GridIndices, tileSize);
    }

    public Box2Rotated GetWorldBounds(TileRef tileRef, Matrix3? worldMatrix = null, Angle? angle = null)
    {
        var grid = _mapManager.GetGrid(tileRef.GridIndex);

        if (worldMatrix == null || angle == null)
        {
            var gridXform = EntityManager.GetComponent<TransformComponent>(grid.GridEntityId);
            var (_, wAng, wMat) = gridXform.GetWorldPositionRotationMatrix();
            worldMatrix = wMat;
            angle = wAng;
        }

        var center = worldMatrix.Value.Transform((Vector2) tileRef.GridIndices + 0.5f) * grid.TileSize;
        var translatedBox = Box2.CenteredAround(center, (grid.TileSize, grid.TileSize));

        return new Box2Rotated(translatedBox, -angle.Value, center);
    }

    #endregion
}
