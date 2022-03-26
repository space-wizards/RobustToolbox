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
    /*
     * There is no GetEntitiesInMap method as this should be avoided; anyone that really needs it can implement it themselves
     */

    private void AddEntitiesIntersecting(
        EntityUid lookupUid,
        HashSet<EntityUid> intersecting,
        Box2 worldAABB,
        LookupFlags flags,
        EntityQuery<EntityLookupComponent> lookupQuery,
        EntityQuery<TransformComponent> xformQuery)
    {
        var lookup = lookupQuery.GetComponent(lookupUid);
        var localAABB = xformQuery.GetComponent(lookupUid).InvWorldMatrix.TransformBox(worldAABB);

        foreach (var ent in lookup.Tree.QueryAabb(localAABB, (flags & LookupFlags.Approximate) != 0x0))
        {
            intersecting.Add(ent);
        }
    }

    private bool AnyEntitiesIntersecting(EntityUid lookupUid,
        Box2 worldAABB,
        LookupFlags flags,
        EntityQuery<EntityLookupComponent> lookupQuery,
        EntityQuery<TransformComponent> xformQuery)
    {
        // TODO:
        var lookup = lookupQuery.GetComponent(lookupUid);
        var localAABB = xformQuery.GetComponent(lookupUid).InvWorldMatrix.TransformBox(worldAABB);
        var found = false;

        lookup.Tree._b2Tree.Query(ref found, static (ref bool state, DynamicTree.Proxy _) =>
        {
            state = true;
            return false;
        }, localAABB);

        if (found)
            return true;
    }

    #region Arc

    public IEnumerable<EntityUid> GetEntitiesInArc(
        EntityCoordinates coordinates,
        float range,
        Angle direction,
        float arcWidth,
        LookupFlags flags = DefaultFlags)
    {
        var position = coordinates.ToMap(EntityManager);

        return GetEntitiesInArc(position, range, direction, arcWidth, flags);
    }

    public IEnumerable<EntityUid> GetEntitiesInArc(
        MapCoordinates coordinates,
        float range,
        Angle direction,
        float arcWidth,
        LookupFlags flags = DefaultFlags)
    {
        var xformQuery = GetEntityQuery<TransformComponent>();

        foreach (var entity in GetEntitiesIntersecting(coordinates, range * 2, flags))
        {
            var angle = new Angle(xformQuery.GetComponent(entity).WorldPosition - coordinates.Position);
            if (angle.Degrees < direction.Degrees + arcWidth / 2 &&
                angle.Degrees > direction.Degrees - arcWidth / 2)
                yield return entity;
        }
    }

    #endregion

    #region Box2

    public bool AnyEntitiesIntersecting(MapId mapId, Box2 worldAABB, LookupFlags flags = DefaultFlags)
    {
        if (mapId == MapId.Nullspace) return false;

        var lookupQuery = GetEntityQuery<EntityLookupComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();

        // Don't need to check contained entities as they have the same bounds as the parent.

        var found = false;
        EntityLookupComponent lookup;
        Box2 localAABB;

        foreach (var grid in _mapManager.FindGridsIntersecting(mapId, worldAABB))
        {

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
        if (mapId == MapId.Nullspace) return Enumerable.Empty<EntityUid>();

        var lookupQuery = GetEntityQuery<EntityLookupComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();

        var intersecting = new HashSet<EntityUid>();

        // Get grid entities
        foreach (var grid in _mapManager.FindGridsIntersecting(mapId, worldAABB))
        {
            AddEntitiesIntersecting(grid.GridEntityId, intersecting, worldAABB, flags, lookupQuery, xformQuery);
        }

        // Get map entities
        var mapUid = _mapManager.GetMapEntityId(mapId);
        AddEntitiesIntersecting(mapUid, intersecting, worldAABB, flags, lookupQuery, xformQuery);

        // TODO: Need to get anchored entities

        // TODO: Need to get contained entities

        return intersecting;
    }

    #endregion

    #region Box2Rotated

    public bool AnyEntitiesIntersecting(MapId mapId, Box2Rotated worldBounds, LookupFlags flags = DefaultFlags)
    {
        var lookupQuery = GetEntityQuery<EntityLookupComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();

        // Don't need to check contained entities as they have the same bounds as the parent.

        var found = false;
        EntityLookupComponent lookup;
        Box2 localAABB;

        foreach (var grid in _mapManager.FindGridsIntersecting(mapId, worldBounds))
        {
            lookup = lookupQuery.GetComponent(grid.GridEntityId);
            localAABB = xformQuery.GetComponent(grid.GridEntityId).InvWorldMatrix.TransformBox(worldBounds);

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
        localAABB = xformQuery.GetComponent(mapUid).InvWorldMatrix.TransformBox(worldBounds);

        lookup.Tree._b2Tree.Query(ref found, static (ref bool state, DynamicTree.Proxy _) =>
        {
            state = true;
            return false;
        }, localAABB);

        return found;
    }

    public IEnumerable<EntityUid> GetEntitiesIntersecting(MapId mapId, Box2Rotated worldBounds, LookupFlags flags = DefaultFlags)
    {
        if (mapId == MapId.Nullspace) return Enumerable.Empty<EntityUid>();

        var lookupQuery = GetEntityQuery<EntityLookupComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();

        var intersecting = new HashSet<EntityUid>();

        // Get grid entities
        foreach (var grid in _mapManager.FindGridsIntersecting(mapId, worldBounds))
        {
            AddEntitiesIntersecting(grid.GridEntityId, intersecting, worldBounds, flags, lookupQuery, xformQuery);
        }

        // Get map entities
        var mapUid = _mapManager.GetMapEntityId(mapId);
        AddEntitiesIntersecting(mapUid, intersecting, worldBounds, flags, lookupQuery, xformQuery);

        // TODO: Need to get anchored entities

        // TODO: Need to get contained entities

        return intersecting;
    }

    #endregion

    #region Entity

    public bool AnyEntitiesIntersecting(EntityUid uid, LookupFlags flags = DefaultFlags)
    {
        var worldAABB = GetWorldAABB(uid);
        var mapID = Transform(uid).MapID;

        return AnyEntitiesIntersecting(mapID, worldAABB, flags);
    }

    #endregion

    #region EntityCoordinates

    public bool AnyEntitiesIntersecting(EntityCoordinates coordinates, LookupFlags flags = DefaultFlags)
    {
        if (!coordinates.IsValid(EntityManager)) return false;

        var mapPos = coordinates.ToMap(EntityManager);
        return AnyEntitiesIntersecting(mapPos, flags);
    }

    public bool AnyEntitiesIntersecting(EntityCoordinates coordinates, float range, LookupFlags flags = DefaultFlags)
    {
        if (!coordinates.IsValid(EntityManager)) return false;

        var mapPos = coordinates.ToMap(EntityManager);
        return AnyEntitiesIntersecting(mapPos, range, flags);
    }

    public IEnumerable<EntityUid> GetEntitiesIntersecting(EntityCoordinates coordinates, LookupFlags flags = DefaultFlags)
    {
        var mapPos = coordinates.ToMap(EntityManager);
        return GetEntitiesIntersecting(mapPos, flags);
    }

    public IEnumerable<EntityUid> GetEntitiesIntersecting(EntityCoordinates coordinates, float range, LookupFlags flags = DefaultFlags)
    {
        var mapPos = coordinates.ToMap(EntityManager);
        return GetEntitiesIntersecting(mapPos, range, flags);
    }

    #endregion

    #region MapCoordinates

    public bool AnyEntitiesIntersecting(MapCoordinates coordinates, LookupFlags flags = DefaultFlags)
    {
        if (coordinates == MapCoordinates.Nullspace) return false;

        var worldAABB = new Box2(coordinates.Position - float.Epsilon, coordinates.Position + float.Epsilon);
        return AnyEntitiesIntersecting(coordinates.MapId, worldAABB, flags);
    }

    public bool AnyEntitiesIntersecting(MapCoordinates coordinates, float range, LookupFlags flags = DefaultFlags)
    {
        // TODO: Actual circles
        if (coordinates == MapCoordinates.Nullspace) return false;

        var worldAABB = new Box2(coordinates.Position - range, coordinates.Position + range);
        return AnyEntitiesIntersecting(coordinates.MapId, worldAABB, flags);
    }

    public IEnumerable<EntityUid> GetEntitiesIntersecting(MapCoordinates coordinates, LookupFlags flags = DefaultFlags)
    {
        if (coordinates == MapCoordinates.Nullspace) return Enumerable.Empty<EntityUid>();

        var worldAABB = new Box2(coordinates.Position - float.Epsilon, coordinates.Position + float.Epsilon);
        return GetEntitiesIntersecting(coordinates.MapId, worldAABB, flags);
    }

    public IEnumerable<EntityUid> GetEntitiesIntersecting(MapCoordinates coordinates, float range,
        LookupFlags flags = DefaultFlags)
    {
        if (coordinates == MapCoordinates.Nullspace) return Enumerable.Empty<EntityUid>();

        // TODO: Actual circles
        var worldAABB = new Box2(coordinates.Position - range, coordinates.Position + range);
        return GetEntitiesIntersecting(coordinates.MapId, worldAABB, flags);
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
