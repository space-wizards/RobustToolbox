using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Robust.Shared.Collections;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;

public sealed partial class EntityLookupSystem
{
    /*
     * There is no GetEntitiesInMap method as this should be avoided; anyone that really needs it can implement it themselves
     */

    // Internal API messy for now but mainly want external to be fairly stable for a while and optimise it later.

    #region Private

    private void AddEntitiesIntersecting(
        EntityUid lookupUid,
        HashSet<EntityUid> intersecting,
        Box2 worldAABB,
        LookupFlags flags,
        EntityQuery<BroadphaseComponent> lookupQuery,
        EntityQuery<TransformComponent> xformQuery)
    {
        var lookup = lookupQuery.GetComponent(lookupUid);
        var invMatrix = _transform.GetInvWorldMatrix(lookupUid, xformQuery);
        var localAABB = invMatrix.TransformBox(worldAABB);

        if ((flags & LookupFlags.Dynamic) != 0x0)
        {
            lookup.DynamicTree.QueryAabb(ref intersecting,
                static (ref HashSet<EntityUid> state, in FixtureProxy value) =>
                {
                    state.Add(value.Fixture.Body.Owner);
                    return true;
                }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        if ((flags & (LookupFlags.Static | LookupFlags.Anchored)) != 0x0)
        {
            lookup.StaticTree.QueryAabb(ref intersecting,
                static (ref HashSet<EntityUid> state, in FixtureProxy value) =>
                {
                    state.Add(value.Fixture.Body.Owner);
                    return true;
                }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        if ((flags & LookupFlags.StaticSundries) == LookupFlags.StaticSundries)
        {
            lookup.StaticSundriesTree.QueryAabb(ref intersecting,
                static (ref HashSet<EntityUid> state, in EntityUid value) =>
                {
                    state.Add(value);
                    return true;
                }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        if ((flags & LookupFlags.Sundries) != 0x0)
        {
            lookup.SundriesTree.QueryAabb(ref intersecting,
                static (ref HashSet<EntityUid> state, in EntityUid value) =>
                {
                    state.Add(value);
                    return true;
                }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }
    }

    private void AddEntitiesIntersecting(
        EntityUid lookupUid,
        HashSet<EntityUid> intersecting,
        Box2Rotated worldBounds,
        LookupFlags flags,
        EntityQuery<BroadphaseComponent> lookupQuery,
        EntityQuery<TransformComponent> xformQuery)
    {
        var lookup = lookupQuery.GetComponent(lookupUid);
        var invMatrix = _transform.GetInvWorldMatrix(lookupUid, xformQuery);
        // We don't just use CalcBoundingBox because the transformed bounds might be tighter.
        var localAABB = invMatrix.TransformBox(worldBounds);

        if ((flags & LookupFlags.Dynamic) != 0x0)
        {
            lookup.DynamicTree.QueryAabb(ref intersecting,
            static (ref HashSet<EntityUid> state, in FixtureProxy value) =>
            {
                state.Add(value.Fixture.Body.Owner);
                return true;
            }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        if ((flags & (LookupFlags.Static | LookupFlags.Anchored)) != 0x0)
        {
            lookup.StaticTree.QueryAabb(ref intersecting,
            static (ref HashSet<EntityUid> state, in FixtureProxy value) =>
            {
                state.Add(value.Fixture.Body.Owner);
                return true;
            }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        if ((flags & LookupFlags.StaticSundries) == LookupFlags.StaticSundries)
        {
            lookup.StaticSundriesTree.QueryAabb(ref intersecting,
                static (ref HashSet<EntityUid> state, in EntityUid value) =>
                {
                    state.Add(value);
                    return true;
                }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        if ((flags & LookupFlags.Sundries) != 0x0)
        {
            lookup.SundriesTree.QueryAabb(ref intersecting,
                static (ref HashSet<EntityUid> state, in EntityUid value) =>
                {
                    state.Add(value);
                    return true;
                }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }
    }

    private bool AnyEntitiesIntersecting(EntityUid lookupUid,
        Box2 worldAABB,
        LookupFlags flags,
        EntityQuery<BroadphaseComponent> lookupQuery,
        EntityQuery<TransformComponent> xformQuery,
        EntityUid? ignored = null)
    {
        var lookup = lookupQuery.GetComponent(lookupUid);
        var localAABB = xformQuery.GetComponent(lookupUid).InvWorldMatrix.TransformBox(worldAABB);
        var state = (ignored, found: false);

        if ((flags & LookupFlags.Dynamic) != 0x0)
        {
            lookup.DynamicTree.QueryAabb(ref state, (ref (EntityUid? ignored, bool found) tuple, in FixtureProxy value) =>
            {
                if (tuple.ignored == value.Fixture.Body.Owner)
                    return true;

                tuple.found = true;
                return false;
            }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        if ((flags & (LookupFlags.Static | LookupFlags.Anchored)) != 0x0)
        {
            lookup.StaticTree.QueryAabb(ref state, (ref (EntityUid? ignored, bool found) tuple, in FixtureProxy value) =>
            {
                if (tuple.ignored == value.Fixture.Body.Owner)
                    return true;

                tuple.found = true;
                return false;
            }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        if ((flags & LookupFlags.StaticSundries) == LookupFlags.StaticSundries)
        {
            lookup.StaticSundriesTree.QueryAabb(ref state, static (ref (EntityUid? ignored, bool found) tuple, in EntityUid value) =>
            {
                if (tuple.ignored == value)
                    return true;

                tuple.found = true;
                return false;
            }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        if ((flags & LookupFlags.Sundries) != 0x0)
        {
            lookup.SundriesTree.QueryAabb(ref state, static (ref (EntityUid? ignored, bool found) tuple, in EntityUid value) =>
            {
                if (tuple.ignored == value)
                    return true;

                tuple.found = true;
                return false;
            }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        if (state.found)
            return true;

        return false;
    }

    private bool AnyEntitiesIntersecting(EntityUid lookupUid,
        Box2Rotated worldBounds,
        LookupFlags flags,
        EntityQuery<BroadphaseComponent> lookupQuery,
        EntityQuery<TransformComponent> xformQuery,
        EntityUid? ignored = null)
    {
        var lookup = lookupQuery.GetComponent(lookupUid);
        var localAABB = xformQuery.GetComponent(lookupUid).InvWorldMatrix.TransformBox(worldBounds);
        var state = (ignored, found: false);

        if ((flags & LookupFlags.Dynamic) != 0x0)
        {
            lookup.DynamicTree.QueryAabb(ref state, (ref (EntityUid? ignored, bool found) tuple, in FixtureProxy value) =>
            {
                if (tuple.ignored == value.Fixture.Body.Owner)
                    return true;

                tuple.found = true;
                return false;
            }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        if ((flags & (LookupFlags.Static | LookupFlags.Anchored)) != 0x0)
        {
            lookup.StaticTree.QueryAabb(ref state, (ref (EntityUid? ignored, bool found) tuple, in FixtureProxy value) =>
            {
                if (tuple.ignored == value.Fixture.Body.Owner)
                    return true;

                tuple.found = true;
                return false;
            }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        if ((flags & LookupFlags.StaticSundries) == LookupFlags.StaticSundries)
        {
            lookup.StaticSundriesTree.QueryAabb(ref state, static (ref (EntityUid? ignored, bool found) tuple, in EntityUid value) =>
            {
                if (tuple.ignored == value)
                    return true;

                tuple.found = true;
                return false;
            }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        if ((flags & LookupFlags.Sundries) != 0x0)
        {
            lookup.SundriesTree.QueryAabb(ref state, static (ref (EntityUid? ignored, bool found) tuple, in EntityUid value) =>
            {
                if (tuple.ignored == value)
                    return true;

                tuple.found = true;
                return false;
            }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        if (state.found)
            return true;

        return state.found;
    }

    private void RecursiveAdd(EntityUid uid, ValueList<EntityUid> toAdd, EntityQuery<TransformComponent> xformQuery)
    {
        var childEnumerator = xformQuery.GetComponent(uid).ChildEnumerator;

        while (childEnumerator.MoveNext(out var child))
        {
            toAdd.Add(child.Value);
            RecursiveAdd(child.Value, toAdd, xformQuery);
        }
    }

    private void AddContained(HashSet<EntityUid> intersecting, LookupFlags flags, EntityQuery<TransformComponent> xformQuery)
    {
        if ((flags & LookupFlags.Contained) == 0x0 || intersecting.Count == 0)
            return;

        var conQuery = GetEntityQuery<ContainerManagerComponent>();
        var toAdd = new ValueList<EntityUid>();

        foreach (var uid in intersecting)
        {
            if (!conQuery.TryGetComponent(uid, out var conManager)) continue;

            foreach (var con in conManager.GetAllContainers())
            {
                foreach (var contained in con.ContainedEntities)
                {
                    toAdd.Add(contained);
                    RecursiveAdd(contained, toAdd, xformQuery);
                }
            }
        }

        foreach (var uid in toAdd)
        {
            intersecting.Add(uid);
        }
    }

    #endregion

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

        foreach (var entity in GetEntitiesInRange(coordinates, range * 2, flags))
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

        var lookupQuery = GetEntityQuery<BroadphaseComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();

        // Don't need to check contained entities as they have the same bounds as the parent.

        foreach (var grid in _mapManager.FindGridsIntersecting(mapId, worldAABB))
        {
            if (AnyEntitiesIntersecting(grid.GridEntityId, worldAABB, flags, lookupQuery, xformQuery)) return true;
        }

        var mapUid = _mapManager.GetMapEntityId(mapId);
        return AnyEntitiesIntersecting(mapUid, worldAABB, flags, lookupQuery, xformQuery);
    }

    public HashSet<EntityUid> GetEntitiesIntersecting(MapId mapId, Box2 worldAABB, LookupFlags flags = DefaultFlags)
    {
        if (mapId == MapId.Nullspace) return new HashSet<EntityUid>();

        var lookupQuery = GetEntityQuery<BroadphaseComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();

        var intersecting = new HashSet<EntityUid>();

        // Get grid entities
        foreach (var grid in _mapManager.FindGridsIntersecting(mapId, worldAABB))
        {
            AddEntitiesIntersecting(grid.GridEntityId, intersecting, worldAABB, flags, lookupQuery, xformQuery);

            if ((flags & (LookupFlags.Anchored | LookupFlags.Static)) != 0x0)
            {
                foreach (var uid in grid.GetAnchoredEntities(worldAABB))
                {
                    if (Deleted(uid)) continue;
                    intersecting.Add(uid);
                }
            }
        }

        // Get map entities
        var mapUid = _mapManager.GetMapEntityId(mapId);
        AddEntitiesIntersecting(mapUid, intersecting, worldAABB, flags, lookupQuery, xformQuery);
        AddContained(intersecting, flags, xformQuery);

        return intersecting;
    }

    #endregion

    #region Box2Rotated

    public bool AnyEntitiesIntersecting(MapId mapId, Box2Rotated worldBounds, LookupFlags flags = DefaultFlags)
    {
        var lookupQuery = GetEntityQuery<BroadphaseComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();

        // Don't need to check contained entities as they have the same bounds as the parent.

        foreach (var grid in _mapManager.FindGridsIntersecting(mapId, worldBounds.CalcBoundingBox()))
        {
            if (AnyEntitiesIntersecting(grid.GridEntityId, worldBounds, flags, lookupQuery, xformQuery)) return true;
        }

        var mapUid = _mapManager.GetMapEntityId(mapId);
        return AnyEntitiesIntersecting(mapUid, worldBounds, flags, lookupQuery, xformQuery);
    }

    public HashSet<EntityUid> GetEntitiesIntersecting(MapId mapId, Box2Rotated worldBounds, LookupFlags flags = DefaultFlags)
    {
        if (mapId == MapId.Nullspace) return new HashSet<EntityUid>();

        var lookupQuery = GetEntityQuery<BroadphaseComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();

        var intersecting = new HashSet<EntityUid>();

        // Get grid entities
        foreach (var grid in _mapManager.FindGridsIntersecting(mapId, worldBounds.CalcBoundingBox()))
        {
            AddEntitiesIntersecting(grid.GridEntityId, intersecting, worldBounds, flags, lookupQuery, xformQuery);
        }

        // Get map entities
        var mapUid = _mapManager.GetMapEntityId(mapId);
        AddEntitiesIntersecting(mapUid, intersecting, worldBounds, flags, lookupQuery, xformQuery);
        AddContained(intersecting, flags, xformQuery);

        return intersecting;
    }

    #endregion

    #region Entity

    // TODO: Bit of duplication between here and the other methods. Was a bit lazy passing around predicates for speed too.

    public bool AnyEntitiesIntersecting(EntityUid uid, LookupFlags flags = DefaultFlags)
    {
        var worldAABB = GetWorldAABB(uid);
        var mapID = Transform(uid).MapID;

        if (mapID == MapId.Nullspace) return false;

        var lookupQuery = GetEntityQuery<BroadphaseComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();

        foreach (var grid in _mapManager.FindGridsIntersecting(mapID, worldAABB))
        {
            if (AnyEntitiesIntersecting(grid.GridEntityId, worldAABB, flags, lookupQuery, xformQuery, uid))
                return true;
        }

        var mapUid = _mapManager.GetMapEntityId(mapID);
        return AnyEntitiesIntersecting(mapUid, worldAABB, flags, lookupQuery, xformQuery, uid);
    }

    public bool AnyEntitiesInRange(EntityUid uid, float range, LookupFlags flags = DefaultFlags)
    {
        var mapPos = Transform(uid).MapPosition;

        if (mapPos.MapId == MapId.Nullspace) return false;

        var worldAABB = new Box2(mapPos.Position - range, mapPos.Position + range);
        var lookupQuery = GetEntityQuery<BroadphaseComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();

        foreach (var grid in _mapManager.FindGridsIntersecting(mapPos.MapId, worldAABB))
        {
            if (AnyEntitiesIntersecting(grid.GridEntityId, worldAABB, flags, lookupQuery, xformQuery, uid))
                return true;
        }

        var mapUid = _mapManager.GetMapEntityId(mapPos.MapId);
        return AnyEntitiesIntersecting(mapUid, worldAABB, flags, lookupQuery, xformQuery, uid);
    }

    public HashSet<EntityUid> GetEntitiesInRange(EntityUid uid, float range, LookupFlags flags = DefaultFlags)
    {
        var mapPos = Transform(uid).MapPosition;

        if (mapPos.MapId == MapId.Nullspace) return new HashSet<EntityUid>();

        var intersecting = GetEntitiesInRange(mapPos, range, flags);
        intersecting.Remove(uid);
        return intersecting;
    }

    public HashSet<EntityUid> GetEntitiesIntersecting(EntityUid uid, LookupFlags flags = DefaultFlags)
    {
        var xform = Transform(uid);
        var mapId = xform.MapID;

        if (mapId == MapId.Nullspace) return new HashSet<EntityUid>();

        var (worldPos, worldRot) = xform.GetWorldPositionRotation();
        var bounds = GetAABBNoContainer(uid, worldPos, worldRot);

        var intersecting = GetEntitiesIntersecting(mapId, bounds, flags);
        intersecting.Remove(uid);
        return intersecting;
    }

    #endregion

    #region EntityCoordinates

    public bool AnyEntitiesIntersecting(EntityCoordinates coordinates, LookupFlags flags = DefaultFlags)
    {
        if (!coordinates.IsValid(EntityManager)) return false;

        var mapPos = coordinates.ToMap(EntityManager);
        return AnyEntitiesIntersecting(mapPos, flags);
    }

    public bool AnyEntitiesInRange(EntityCoordinates coordinates, float range, LookupFlags flags = DefaultFlags)
    {
        if (!coordinates.IsValid(EntityManager)) return false;

        var mapPos = coordinates.ToMap(EntityManager);
        return AnyEntitiesInRange(mapPos, range, flags);
    }

    public HashSet<EntityUid> GetEntitiesIntersecting(EntityCoordinates coordinates, LookupFlags flags = DefaultFlags)
    {
        var mapPos = coordinates.ToMap(EntityManager);
        return GetEntitiesIntersecting(mapPos, flags);
    }

    public HashSet<EntityUid> GetEntitiesInRange(EntityCoordinates coordinates, float range, LookupFlags flags = DefaultFlags)
    {
        var mapPos = coordinates.ToMap(EntityManager);
        return GetEntitiesInRange(mapPos, range, flags);
    }

    #endregion

    #region MapCoordinates

    public bool AnyEntitiesIntersecting(MapCoordinates coordinates, LookupFlags flags = DefaultFlags)
    {
        if (coordinates.MapId == MapId.Nullspace) return false;

        var worldAABB = new Box2(coordinates.Position - float.Epsilon, coordinates.Position + float.Epsilon);
        return AnyEntitiesIntersecting(coordinates.MapId, worldAABB, flags);
    }

    public bool AnyEntitiesInRange(MapCoordinates coordinates, float range, LookupFlags flags = DefaultFlags)
    {
        // TODO: Actual circles
        if (coordinates.MapId == MapId.Nullspace) return false;

        var worldAABB = new Box2(coordinates.Position - range, coordinates.Position + range);
        return AnyEntitiesIntersecting(coordinates.MapId, worldAABB, flags);
    }

    public HashSet<EntityUid> GetEntitiesIntersecting(MapCoordinates coordinates, LookupFlags flags = DefaultFlags)
    {
        if (coordinates.MapId == MapId.Nullspace) return new HashSet<EntityUid>();

        var worldAABB = new Box2(coordinates.Position - float.Epsilon, coordinates.Position + float.Epsilon);
        return GetEntitiesIntersecting(coordinates.MapId, worldAABB, flags);
    }

    public HashSet<EntityUid> GetEntitiesInRange(MapCoordinates coordinates, float range, LookupFlags flags = DefaultFlags)
    {
        return GetEntitiesInRange(coordinates.MapId, coordinates.Position, range, flags);
    }

    #endregion

    #region MapId

    public HashSet<EntityUid> GetEntitiesInRange(MapId mapId, Vector2 worldPos, float range,
        LookupFlags flags = DefaultFlags)
    {
        DebugTools.Assert(range > 0, "Range must be a positive float");

        if (mapId == MapId.Nullspace) return new HashSet<EntityUid>();

        // TODO: Actual circles
        var worldAABB = new Box2(worldPos - range, worldPos + range);
        return GetEntitiesIntersecting(mapId, worldAABB, flags);
    }

    #endregion

    #region Grid Methods

    /// <summary>
    /// Returns the entities intersecting any of the supplied tiles. Faster than doing each tile individually.
    /// </summary>
    /// <param name="gridId"></param>
    /// <param name="gridIndices"></param>
    /// <returns></returns>
    public HashSet<EntityUid> GetEntitiesIntersecting(EntityUid gridId, IEnumerable<Vector2i> gridIndices, LookupFlags flags = DefaultFlags)
    {
        // Technically this doesn't consider anything overlapping from outside the grid but is this an issue?
        if (!_mapManager.TryGetGrid(gridId, out var grid)) return new HashSet<EntityUid>();

        var lookup = Comp<BroadphaseComponent>(grid.GridEntityId);
        var intersecting = new HashSet<EntityUid>();
        var tileSize = grid.TileSize;

        // TODO: You can probably decompose the indices into larger areas if you take in a hashset instead.
        foreach (var index in gridIndices)
        {
            var aabb = GetLocalBounds(index, tileSize);

            if ((flags & LookupFlags.Dynamic) != 0x0)
            {
                lookup.DynamicTree.QueryAabb(ref intersecting, static (ref HashSet<EntityUid> state, in FixtureProxy value) =>
                {
                    state.Add(value.Fixture.Body.Owner);
                    return true;
                }, aabb, (flags & LookupFlags.Approximate) != 0x0);
            }

            if ((flags & (LookupFlags.Static | LookupFlags.Anchored)) != 0x0)
            {
                lookup.StaticTree.QueryAabb(ref intersecting, static (ref HashSet<EntityUid> state, in FixtureProxy value) =>
                {
                    state.Add(value.Fixture.Body.Owner);
                    return true;
                }, aabb, (flags & LookupFlags.Approximate) != 0x0);
            }

            if ((flags & LookupFlags.StaticSundries) == LookupFlags.StaticSundries)
            {
                lookup.StaticSundriesTree.QueryAabb(ref intersecting, static (ref HashSet<EntityUid> intersecting, in EntityUid value) =>
                {
                    intersecting.Add(value);
                    return true;
                }, aabb, (flags & LookupFlags.Approximate) != 0x0);
            }

            if ((flags & LookupFlags.Sundries) != 0x0)
            {
                lookup.SundriesTree.QueryAabb(ref intersecting, static (ref HashSet<EntityUid> intersecting, in EntityUid value) =>
                {
                    intersecting.Add(value);
                    return true;
                }, aabb, (flags & LookupFlags.Approximate) != 0x0);
            }
        }

        var xformQuery = GetEntityQuery<TransformComponent>();
        AddContained(intersecting, flags, xformQuery);

        return intersecting;
    }

    public HashSet<EntityUid> GetEntitiesIntersecting(EntityUid gridId, Vector2i gridIndices, LookupFlags flags = DefaultFlags)
    {
        // Technically this doesn't consider anything overlapping from outside the grid but is this an issue?
        if (!_mapManager.TryGetGrid(gridId, out var grid)) return new HashSet<EntityUid>();
        var lookup = Comp<BroadphaseComponent>(grid.GridEntityId);
        var tileSize = grid.TileSize;
        var aabb = GetLocalBounds(gridIndices, tileSize);
        return GetEntitiesIntersecting(lookup, aabb, flags);
    }

    public HashSet<EntityUid> GetEntitiesIntersecting(BroadphaseComponent lookup, Box2 aabb, LookupFlags flags = DefaultFlags)
    {
        var intersecting = new HashSet<EntityUid>();

        if ((flags & LookupFlags.Dynamic) != 0x0)
        {
            lookup.DynamicTree.QueryAabb(ref intersecting,
                static (ref HashSet<EntityUid> intersecting, in FixtureProxy value) =>
                {
                    intersecting.Add(value.Fixture.Body.Owner);
                    return true;
                }, aabb, (flags & LookupFlags.Approximate) != 0x0);
        }

        if ((flags & (LookupFlags.Static | LookupFlags.Anchored)) != 0x0)
        {
            lookup.StaticTree.QueryAabb(ref intersecting,
                static (ref HashSet<EntityUid> intersecting, in FixtureProxy value) =>
                {
                    intersecting.Add(value.Fixture.Body.Owner);
                    return true;
                }, aabb, (flags & LookupFlags.Approximate) != 0x0);
        }

        var state = (lookup.StaticSundriesTree._b2Tree, intersecting);
        if ((flags & LookupFlags.StaticSundries) == LookupFlags.StaticSundries)
        {
            lookup.StaticSundriesTree._b2Tree.Query(ref state, static (ref (B2DynamicTree<EntityUid> _b2Tree, HashSet<EntityUid> intersecting) tuple, DynamicTree.Proxy proxy) =>
            {
                tuple.intersecting.Add(tuple._b2Tree.GetUserData(proxy));
                return true;
            }, aabb);
        }

        state = (lookup.SundriesTree._b2Tree, intersecting);
        if ((flags & LookupFlags.Sundries) != 0x0)
        {
            lookup.SundriesTree._b2Tree.Query(ref state, static (ref (B2DynamicTree<EntityUid> _b2Tree, HashSet<EntityUid> intersecting) tuple, DynamicTree.Proxy proxy) =>
            {
                tuple.intersecting.Add(tuple._b2Tree.GetUserData(proxy));
                return true;
            }, aabb);
        }

        var xformQuery = GetEntityQuery<TransformComponent>();
        AddContained(intersecting, flags, xformQuery);

        return intersecting;
    }

    public HashSet<EntityUid> GetEntitiesIntersecting(EntityUid gridId, Box2 worldAABB, LookupFlags flags = DefaultFlags)
    {
        if (!_mapManager.TryGetGrid(gridId, out var grid)) return new HashSet<EntityUid>();

        var lookupQuery = GetEntityQuery<BroadphaseComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();
        var intersecting = new HashSet<EntityUid>();

        AddEntitiesIntersecting(gridId, intersecting, worldAABB, flags, lookupQuery, xformQuery);
        AddContained(intersecting, flags, xformQuery);

        return intersecting;
    }

    public HashSet<EntityUid> GetEntitiesIntersecting(EntityUid gridId, Box2Rotated worldBounds, LookupFlags flags = DefaultFlags)
    {
        if (!_mapManager.TryGetGrid(gridId, out var grid)) return new HashSet<EntityUid>();

        var lookupQuery = GetEntityQuery<BroadphaseComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();
        var intersecting = new HashSet<EntityUid>();

        AddEntitiesIntersecting(grid.GridEntityId, intersecting, worldBounds, flags, lookupQuery, xformQuery);
        AddContained(intersecting, flags, xformQuery);

        return intersecting;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerable<EntityUid> GetEntitiesIntersecting(TileRef tileRef, LookupFlags flags = DefaultFlags)
    {
        return GetEntitiesIntersecting(tileRef.GridUid, tileRef.GridIndices, flags);
    }

    #endregion

    #region Lookup Query

    public HashSet<EntityUid> GetEntitiesIntersecting(BroadphaseComponent component, ref Box2 worldAABB, LookupFlags flags = DefaultFlags)
    {
        var intersecting = new HashSet<EntityUid>();
        var xformQuery = GetEntityQuery<TransformComponent>();
        var localAABB = xformQuery.GetComponent(component.Owner).InvWorldMatrix.TransformBox(worldAABB);

        if ((flags & LookupFlags.Dynamic) != 0x0)
        {
            component.DynamicTree.QueryAabb(ref intersecting, static (ref HashSet<EntityUid> intersecting, in FixtureProxy value) =>
            {
                intersecting.Add(value.Fixture.Body.Owner);
                return true;
            }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        if ((flags & (LookupFlags.Static | LookupFlags.Anchored)) != 0x0)
        {
            component.StaticTree.QueryAabb(ref intersecting, static (ref HashSet<EntityUid> intersecting, in FixtureProxy value) =>
            {
                intersecting.Add(value.Fixture.Body.Owner);
                return true;
            }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        if ((flags & LookupFlags.StaticSundries) == LookupFlags.StaticSundries)
        {
            component.StaticSundriesTree.QueryAabb(ref intersecting, static (ref HashSet<EntityUid> intersecting, in EntityUid value) =>
            {
                intersecting.Add(value);
                return true;
            }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        if ((flags & LookupFlags.Sundries) != 0x0)
        {
            component.SundriesTree.QueryAabb(ref intersecting, static (ref HashSet<EntityUid> intersecting, in EntityUid value) =>
            {
                intersecting.Add(value);
                return true;
            }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        AddContained(intersecting, flags, xformQuery);

        return intersecting;
    }

    public HashSet<EntityUid> GetLocalEntitiesIntersecting(BroadphaseComponent component, Box2 localAABB, LookupFlags flags = DefaultFlags)
    {
        var intersecting = new HashSet<EntityUid>();

        if ((flags & LookupFlags.Dynamic) != 0x0)
        {
            component.DynamicTree.QueryAabb(ref intersecting, static (ref HashSet<EntityUid> intersecting, in FixtureProxy value) =>
            {
                intersecting.Add(value.Fixture.Body.Owner);
                return true;
            }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        if ((flags & (LookupFlags.Static | LookupFlags.Anchored)) != 0x0)
        {
            component.StaticTree.QueryAabb(ref intersecting, static (ref HashSet<EntityUid> intersecting, in FixtureProxy value) =>
            {
                intersecting.Add(value.Fixture.Body.Owner);
                return true;
            }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        if ((flags & LookupFlags.StaticSundries) == LookupFlags.StaticSundries)
        {
            component.StaticSundriesTree.QueryAabb(ref intersecting, static (ref HashSet<EntityUid> intersecting, in EntityUid value) =>
            {
                intersecting.Add(value);
                return true;
            }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        if ((flags & LookupFlags.Sundries) != 0x0)
        {
            component.SundriesTree.QueryAabb(ref intersecting, static (ref HashSet<EntityUid> intersecting, in EntityUid value) =>
            {
                intersecting.Add(value);
                return true;
            }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        AddContained(intersecting, flags, GetEntityQuery<TransformComponent>());

        return intersecting;
    }

    #endregion

    #region Lookups

    /// <summary>
    /// Gets the relevant <see cref="BroadphaseComponent"/> that intersects the specified area.
    /// </summary>
    public IEnumerable<BroadphaseComponent> FindLookupsIntersecting(MapId mapId, Box2 worldAABB)
    {
        if (mapId == MapId.Nullspace) yield break;

        var lookupQuery = GetEntityQuery<BroadphaseComponent>();

        yield return lookupQuery.GetComponent(_mapManager.GetMapEntityId(mapId));

        foreach (var grid in _mapManager.FindGridsIntersecting(mapId, worldAABB))
        {
            yield return lookupQuery.GetComponent(grid.GridEntityId);
        }
    }

    /// <summary>
    /// Gets the relevant <see cref="BroadphaseComponent"/> that intersects the specified area.
    /// </summary>
    public IEnumerable<BroadphaseComponent> FindLookupsIntersecting(MapId mapId, Box2Rotated worldBounds)
    {
        if (mapId == MapId.Nullspace) yield break;

        var lookupQuery = GetEntityQuery<BroadphaseComponent>();

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
        var grid = _mapManager.GetGrid(tileRef.GridUid);

        if (worldMatrix == null || angle == null)
        {
            var gridXform = Transform(grid.GridEntityId);
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
