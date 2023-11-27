using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
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
        Box2 localAABB,
        LookupFlags flags)
    {
        var lookup = _broadQuery.GetComponent(lookupUid);
        var state = (intersecting, flags);

        if ((flags & LookupFlags.Dynamic) != 0x0)
        {
            lookup.DynamicTree.QueryAabb(ref state,
                static (ref (HashSet<EntityUid> intersecting, LookupFlags flags) tuple, in FixtureProxy value) =>
                {
                    if ((tuple.flags & LookupFlags.Sensors) == 0x0 && !value.Fixture.Hard)
                        return true;

                    tuple.intersecting.Add(value.Entity);
                    return true;
                }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        if ((flags & LookupFlags.Static) != 0x0)
        {
            lookup.StaticTree.QueryAabb(ref state,
                static (ref (HashSet<EntityUid> intersecting, LookupFlags flags) tuple, in FixtureProxy value) =>
                {
                    if ((tuple.flags & LookupFlags.Sensors) == 0x0 && !value.Fixture.Hard)
                        return true;

                    tuple.intersecting.Add(value.Entity);
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
        LookupFlags flags)
    {
        var invMatrix = _transform.GetInvWorldMatrix(lookupUid);
        // We don't just use CalcBoundingBox because the transformed bounds might be tighter.
        var localAABB = invMatrix.TransformBox(worldBounds);

        // Someday we'll split these but maybe it's wishful thinking.
        AddEntitiesIntersecting(lookupUid, intersecting, localAABB, flags);
    }

    private bool AnyEntitiesIntersecting(EntityUid lookupUid,
        Box2 worldAABB,
        LookupFlags flags,
        EntityUid? ignored = null)
    {
        var lookup = _broadQuery.GetComponent(lookupUid);
        var localAABB = _transform.GetInvWorldMatrix(lookupUid).TransformBox(worldAABB);
        var state = (ignored, flags, found: false);

        if ((flags & LookupFlags.Dynamic) != 0x0)
        {
            lookup.DynamicTree.QueryAabb(ref state, static (ref (EntityUid? ignored, LookupFlags flags, bool found) tuple, in FixtureProxy value) =>
            {
                if (tuple.ignored == value.Entity || ((tuple.flags & LookupFlags.Sensors) == 0x0 && !value.Fixture.Hard))
                    return true;

                tuple.found = true;
                return false;
            }, localAABB, (flags & LookupFlags.Approximate) != 0x0);

            if (state.found)
                return true;
        }

        if ((flags & LookupFlags.Static) != 0x0)
        {
            lookup.StaticTree.QueryAabb(ref state, static (ref (EntityUid? ignored, LookupFlags flags, bool found) tuple, in FixtureProxy value) =>
            {
                if (tuple.ignored == value.Entity || ((tuple.flags & LookupFlags.Sensors) == 0x0 && !value.Fixture.Hard))
                    return true;

                tuple.found = true;
                return false;
            }, localAABB, (flags & LookupFlags.Approximate) != 0x0);

            if (state.found)
                return true;
        }

        if ((flags & LookupFlags.StaticSundries) == LookupFlags.StaticSundries)
        {
            lookup.StaticSundriesTree.QueryAabb(ref state, static (ref (EntityUid? ignored, LookupFlags flags, bool found) tuple, in EntityUid value) =>
            {
                if (tuple.ignored == value)
                    return true;

                tuple.found = true;
                return false;
            }, localAABB, (flags & LookupFlags.Approximate) != 0x0);

            if (state.found)
                return true;
        }

        if ((flags & LookupFlags.Sundries) != 0x0)
        {
            lookup.SundriesTree.QueryAabb(ref state, static (ref (EntityUid? ignored, LookupFlags flags, bool found) tuple, in EntityUid value) =>
            {
                if (tuple.ignored == value)
                    return true;

                tuple.found = true;
                return false;
            }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        return state.found;
    }

    private bool AnyEntitiesIntersecting(EntityUid lookupUid,
        Box2Rotated worldBounds,
        LookupFlags flags,
        EntityUid? ignored = null)
    {
        var localAABB = _transform.GetInvWorldMatrix(lookupUid).TransformBox(worldBounds);
        return AnyEntitiesIntersecting(lookupUid, localAABB, flags, ignored);
    }

    private void RecursiveAdd(EntityUid uid, ref ValueList<EntityUid> toAdd)
    {
        if (!_xformQuery.TryGetComponent(uid, out var xform))
        {
            Log.Error($"Encountered deleted entity {uid} while performing entity lookup.");
            return;
        }

        toAdd.Add(uid);
        var childEnumerator = xform.ChildEnumerator;
        while (childEnumerator.MoveNext(out var child))
        {
            RecursiveAdd(child.Value, ref toAdd);
        }
    }

    private void AddContained(HashSet<EntityUid> intersecting, LookupFlags flags)
    {
        if ((flags & LookupFlags.Contained) == 0x0 || intersecting.Count == 0)
            return;

        // TODO PERFORMANCE.
        // toAdd only exists because we can't add directly to intersecting w/o enumeration issues.
        // If we assume that there are more entities in containers than there are entities in the intersecting set, then
        // we would be better off creating a fixed-size EntityUid array and coping all intersecting entities into that
        // instead of creating a value list here that needs to be resized.
        var toAdd = new ValueList<EntityUid>();

        foreach (var uid in intersecting)
        {
            if (!_containerQuery.TryGetComponent(uid, out var conManager))
                continue;

            foreach (var con in conManager.GetAllContainers())
            {
                foreach (var contained in con.ContainedEntities)
                {
                    RecursiveAdd(contained, ref toAdd);
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
        var position = coordinates.ToMap(EntityManager, _transform);

        return GetEntitiesInArc(position, range, direction, arcWidth, flags);
    }

    public IEnumerable<EntityUid> GetEntitiesInArc(
        MapCoordinates coordinates,
        float range,
        Angle direction,
        float arcWidth,
        LookupFlags flags = DefaultFlags)
    {
        foreach (var entity in GetEntitiesInRange(coordinates, range * 2, flags))
        {
            var angle = new Angle(_transform.GetWorldPosition(entity) - coordinates.Position);
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

        // Don't need to check contained entities as they have the same bounds as the parent.
        var found = false;

        var state = (this, worldAABB, flags, found);

        _mapManager.FindGridsIntersecting(mapId, worldAABB, ref state,
            static (EntityUid uid, MapGridComponent _, ref (EntityLookupSystem lookup, Box2 worldAABB, LookupFlags flags, bool found) tuple) =>
            {
                if (!tuple.lookup.AnyEntitiesIntersecting(uid, tuple.worldAABB, tuple.flags))
                    return true;

                tuple.found = true;
                return false;
            });

        if (state.found)
            return true;

        var mapUid = _mapManager.GetMapEntityId(mapId);
        return AnyEntitiesIntersecting(mapUid, worldAABB, flags);
    }

    public HashSet<EntityUid> GetEntitiesIntersecting(MapId mapId, Box2 worldAABB, LookupFlags flags = DefaultFlags)
    {
        var intersecting = new HashSet<EntityUid>();
        GetEntitiesIntersecting(mapId, worldAABB, intersecting, flags);
        return intersecting;
    }

    public void GetEntitiesIntersecting(MapId mapId, Box2 worldAABB, HashSet<EntityUid> intersecting, LookupFlags flags = DefaultFlags)
    {
        if (mapId == MapId.Nullspace) return;

        // Get grid entities
        var state = (this, _map, intersecting, worldAABB, _transform, flags);

        _mapManager.FindGridsIntersecting(mapId, worldAABB, ref state,
            static (EntityUid gridUid, MapGridComponent grid, ref (
                EntityLookupSystem lookup, SharedMapSystem _map, HashSet<EntityUid> intersecting,
                Box2 worldAABB, SharedTransformSystem xformSystem, LookupFlags flags) tuple) =>
            {
                var localAABB = tuple.xformSystem.GetInvWorldMatrix(gridUid).TransformBox(tuple.worldAABB);
                tuple.lookup.AddEntitiesIntersecting(gridUid, tuple.intersecting, localAABB, tuple.flags);

                if ((tuple.flags & LookupFlags.Static) != 0x0)
                {
                    // TODO: Need a struct enumerator version.
                    foreach (var uid in tuple._map.GetAnchoredEntities(gridUid, grid, tuple.worldAABB))
                    {
                        if (tuple.lookup.Deleted(uid))
                            continue;

                        tuple.intersecting.Add(uid);
                    }
                }

                return true;
            });

        // Get map entities
        var mapUid = _mapManager.GetMapEntityId(mapId);
        // Transform just in case future proofing?
        var localAABB = _transform.GetInvWorldMatrix(mapUid).TransformBox(worldAABB);
        AddEntitiesIntersecting(mapUid, intersecting, localAABB, flags);
        AddContained(intersecting, flags);
    }

    #endregion

    #region Box2Rotated

    public bool AnyEntitiesIntersecting(MapId mapId, Box2Rotated worldBounds, LookupFlags flags = DefaultFlags)
    {
        // Don't need to check contained entities as they have the same bounds as the parent.
        var worldAABB = worldBounds.CalcBoundingBox();

        const bool found = false;
        var state = (this, worldBounds, flags, found);

        _mapManager.FindGridsIntersecting(mapId, worldAABB, ref state,
            static (EntityUid uid, MapGridComponent grid, ref (EntityLookupSystem lookup, Box2Rotated worldBounds, LookupFlags flags, bool found) tuple) =>
            {
                if (tuple.lookup.AnyEntitiesIntersecting(uid, tuple.worldBounds, tuple.flags))
                {
                    tuple.found = true;
                    return false;
                }
                return true;
            });

        if (state.found)
            return true;

        var mapUid = _mapManager.GetMapEntityId(mapId);
        return AnyEntitiesIntersecting(mapUid, worldBounds, flags);
    }

    public HashSet<EntityUid> GetEntitiesIntersecting(MapId mapId, Box2Rotated worldBounds, LookupFlags flags = DefaultFlags)
    {
        var intersecting = new HashSet<EntityUid>();

        if (mapId == MapId.Nullspace)
            return intersecting;

        // Get grid entities
        var state = (this, intersecting, worldBounds, flags);

        _mapManager.FindGridsIntersecting(mapId, worldBounds.CalcBoundingBox(), ref state, static
        (EntityUid uid, MapGridComponent _,
            ref (EntityLookupSystem lookup,
                HashSet<EntityUid> intersecting,
                Box2Rotated worldBounds,
                LookupFlags flags) tuple) =>
        {
            tuple.lookup.AddEntitiesIntersecting(uid, tuple.intersecting, tuple.worldBounds, tuple.flags);
            return true;
        });

        // Get map entities
        var mapUid = _mapManager.GetMapEntityId(mapId);
        AddEntitiesIntersecting(mapUid, intersecting, worldBounds, flags);
        AddContained(intersecting, flags);

        return intersecting;
    }

    #endregion

    #region Entity

    // TODO: Bit of duplication between here and the other methods. Was a bit lazy passing around predicates for speed too.

    public bool AnyEntitiesIntersecting(EntityUid uid, LookupFlags flags = DefaultFlags)
    {
        var worldAABB = GetWorldAABB(uid);
        var mapID = _xformQuery.GetComponent(uid).MapID;

        if (mapID == MapId.Nullspace)
            return false;

        const bool found = false;
        var state = (this, worldAABB, flags, found, uid);

        _mapManager.FindGridsIntersecting(mapID, worldAABB, ref state,
            static (EntityUid gridUid, MapGridComponent grid,
                ref (EntityLookupSystem lookup, Box2 worldAABB, LookupFlags flags, bool found, EntityUid ignored) tuple) =>
            {
                if (tuple.lookup.AnyEntitiesIntersecting(gridUid, tuple.worldAABB, tuple.flags, tuple.ignored))
                {
                    tuple.found = true;
                    return false;
                }

                return true;
            });

        var mapUid = _mapManager.GetMapEntityId(mapID);
        return AnyEntitiesIntersecting(mapUid, worldAABB, flags, uid);
    }

    public bool AnyEntitiesInRange(EntityUid uid, float range, LookupFlags flags = DefaultFlags)
    {
        var mapPos = _xformQuery.GetComponent(uid).MapPosition;

        if (mapPos.MapId == MapId.Nullspace)
            return false;

        var rangeVec = new Vector2(range, range);
        var worldAABB = new Box2(mapPos.Position - rangeVec, mapPos.Position + rangeVec);

        const bool found = false;
        var state = (this, worldAABB, flags, found, uid);

        _mapManager.FindGridsIntersecting(mapPos.MapId, worldAABB, ref state, static (
            EntityUid gridUid,
            MapGridComponent _, ref (
                EntityLookupSystem lookup,
                Box2 worldAABB,
                LookupFlags flags,
                bool found,
                EntityUid ignored) tuple) =>
        {
            if (tuple.lookup.AnyEntitiesIntersecting(gridUid, tuple.worldAABB, tuple.flags, tuple.ignored))
            {
                tuple.found = true;
                return false;
            }

            return true;
        });

        var mapUid = _mapManager.GetMapEntityId(mapPos.MapId);
        return AnyEntitiesIntersecting(mapUid, worldAABB, flags, uid);
    }

    public HashSet<EntityUid> GetEntitiesInRange(EntityUid uid, float range, LookupFlags flags = DefaultFlags)
    {
        var mapPos = _xformQuery.GetComponent(uid).MapPosition;

        if (mapPos.MapId == MapId.Nullspace)
            return new HashSet<EntityUid>();

        var intersecting = GetEntitiesInRange(mapPos, range, flags);
        intersecting.Remove(uid);
        return intersecting;
    }

    public HashSet<EntityUid> GetEntitiesIntersecting(EntityUid uid, LookupFlags flags = DefaultFlags)
    {
        var xform = _xformQuery.GetComponent(uid);
        var mapId = xform.MapID;

        if (mapId == MapId.Nullspace)
            return new HashSet<EntityUid>();

        var (worldPos, worldRot) = _transform.GetWorldPositionRotation(xform);
        var bounds = GetAABBNoContainer(uid, worldPos, worldRot);

        var intersecting = GetEntitiesIntersecting(mapId, bounds, flags);
        intersecting.Remove(uid);
        return intersecting;
    }

    #endregion

    #region EntityCoordinates

    public bool AnyEntitiesIntersecting(EntityCoordinates coordinates, LookupFlags flags = DefaultFlags)
    {
        if (!coordinates.IsValid(EntityManager))
            return false;

        var mapPos = coordinates.ToMap(EntityManager, _transform);
        return AnyEntitiesIntersecting(mapPos, flags);
    }

    public bool AnyEntitiesInRange(EntityCoordinates coordinates, float range, LookupFlags flags = DefaultFlags)
    {
        if (!coordinates.IsValid(EntityManager))
            return false;

        var mapPos = coordinates.ToMap(EntityManager, _transform);
        return AnyEntitiesInRange(mapPos, range, flags);
    }

    public HashSet<EntityUid> GetEntitiesIntersecting(EntityCoordinates coordinates, LookupFlags flags = DefaultFlags)
    {
        var mapPos = coordinates.ToMap(EntityManager, _transform);
        return GetEntitiesIntersecting(mapPos, flags);
    }

    public HashSet<EntityUid> GetEntitiesInRange(EntityCoordinates coordinates, float range, LookupFlags flags = DefaultFlags)
    {
        var mapPos = coordinates.ToMap(EntityManager, _transform);
        return GetEntitiesInRange(mapPos, range, flags);
    }

    #endregion

    #region MapCoordinates

    public bool AnyEntitiesIntersecting(MapCoordinates coordinates, LookupFlags flags = DefaultFlags)
    {
        if (coordinates.MapId == MapId.Nullspace) return false;

        var rangeVec = new Vector2(float.Epsilon, float.Epsilon);
        var worldAABB = new Box2(coordinates.Position - rangeVec, coordinates.Position + rangeVec);
        return AnyEntitiesIntersecting(coordinates.MapId, worldAABB, flags);
    }

    public bool AnyEntitiesInRange(MapCoordinates coordinates, float range, LookupFlags flags = DefaultFlags)
    {
        // TODO: Actual circles
        if (coordinates.MapId == MapId.Nullspace) return false;

        var rangeVec = new Vector2(range, range);
        var worldAABB = new Box2(coordinates.Position - rangeVec, coordinates.Position + rangeVec);
        return AnyEntitiesIntersecting(coordinates.MapId, worldAABB, flags);
    }

    public HashSet<EntityUid> GetEntitiesIntersecting(MapCoordinates coordinates, LookupFlags flags = DefaultFlags)
    {
        if (coordinates.MapId == MapId.Nullspace) return new HashSet<EntityUid>();

        var rangeVec = new Vector2(float.Epsilon, float.Epsilon);
        var worldAABB = new Box2(coordinates.Position - rangeVec, coordinates.Position + rangeVec);
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
        var entities = new HashSet<EntityUid>();
        GetEntitiesInRange(mapId, worldPos, range, entities, flags);
        return entities;
    }

    public void GetEntitiesInRange(MapId mapId, Vector2 worldPos, float range, HashSet<EntityUid> entities, LookupFlags flags = DefaultFlags)
    {
        DebugTools.Assert(range > 0, "Range must be a positive float");

        if (mapId == MapId.Nullspace)
            return;

        // TODO: Actual circles
        var rangeVec = new Vector2(range, range);
        var worldAABB = new Box2(worldPos - rangeVec, worldPos + rangeVec);
        GetEntitiesIntersecting(mapId, worldAABB, entities, flags);
    }

    #endregion

    #region Grid Methods

    /// <summary>
    /// Returns the entities intersecting any of the supplied tiles. Faster than doing each tile individually.
    /// </summary>
    public HashSet<EntityUid> GetEntitiesIntersecting(EntityUid gridId, IEnumerable<Vector2i> gridIndices, LookupFlags flags = DefaultFlags)
    {
        // Technically this doesn't consider anything overlapping from outside the grid but is this an issue?
        if (!_mapManager.TryGetGrid(gridId, out var grid)) return new HashSet<EntityUid>();

        var lookup = _broadQuery.GetComponent(gridId);
        var intersecting = new HashSet<EntityUid>();
        var tileSize = grid.TileSize;

        // TODO: You can probably decompose the indices into larger areas if you take in a hashset instead.
        foreach (var index in gridIndices)
        {
            var aabb = GetLocalBounds(index, tileSize);
            intersecting.UnionWith(GetEntitiesIntersecting(lookup, aabb, flags));
        }

        AddContained(intersecting, flags);

        return intersecting;
    }

    public HashSet<EntityUid> GetEntitiesIntersecting(EntityUid gridId, Vector2i gridIndices, float enlargement = TileEnlargementRadius, LookupFlags flags = DefaultFlags)
    {
        // Technically this doesn't consider anything overlapping from outside the grid but is this an issue?
        if (!_mapManager.TryGetGrid(gridId, out var grid))
            return new HashSet<EntityUid>();

        var lookup = _broadQuery.GetComponent(gridId);
        var tileSize = grid.TileSize;
        var aabb = GetLocalBounds(gridIndices, tileSize);
        aabb = aabb.Enlarged(enlargement);
        return GetEntitiesIntersecting(lookup, aabb, flags);
    }

    public HashSet<EntityUid> GetEntitiesIntersecting(BroadphaseComponent lookup, Box2 localAABB, LookupFlags flags = DefaultFlags)
    {
        var intersecting = new HashSet<EntityUid>();
        // Dummy tree
        var state = (lookup.StaticSundriesTree._b2Tree, intersecting, flags);

        if ((flags & LookupFlags.Dynamic) != 0x0)
        {
            lookup.DynamicTree.QueryAabb(ref state,
                static (ref (B2DynamicTree<EntityUid> _, HashSet<EntityUid> intersecting, LookupFlags flags) tuple,
                    in FixtureProxy value) =>
                {
                    if ((tuple.flags & LookupFlags.Sensors) != 0x0 || value.Fixture.Hard)
                    {
                        tuple.intersecting.Add(value.Entity);
                    }

                    return true;
                }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        if ((flags & LookupFlags.Static) != 0x0)
        {
            lookup.StaticTree.QueryAabb(ref state,
                static (ref (B2DynamicTree<EntityUid> _, HashSet<EntityUid> intersecting, LookupFlags flags) tuple,
                    in FixtureProxy value) =>
                {
                    if ((tuple.flags & LookupFlags.Sensors) != 0x0 || value.Fixture.Hard)
                    {
                        tuple.intersecting.Add(value.Entity);
                    }

                    return true;
                }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        if ((flags & LookupFlags.StaticSundries) == LookupFlags.StaticSundries)
        {
            state = (lookup.StaticSundriesTree._b2Tree, intersecting, flags);

            lookup.StaticSundriesTree._b2Tree.Query(ref state,
                static (ref (B2DynamicTree<EntityUid> _b2Tree, HashSet<EntityUid> intersecting, LookupFlags flags) tuple,
                    DynamicTree.Proxy proxy) =>
                {
                    tuple.intersecting.Add(tuple._b2Tree.GetUserData(proxy));
                    return true;
                }, localAABB);
        }

        if ((flags & LookupFlags.Sundries) != 0x0)
        {
            state = (lookup.SundriesTree._b2Tree, intersecting, flags);

            lookup.SundriesTree._b2Tree.Query(ref state,
                static (ref (B2DynamicTree<EntityUid> _b2Tree, HashSet<EntityUid> intersecting, LookupFlags flags) tuple,
                    DynamicTree.Proxy proxy) =>
                {
                    tuple.intersecting.Add(tuple._b2Tree.GetUserData(proxy));
                    return true;
                }, localAABB);
        }

        AddContained(intersecting, flags);

        return intersecting;
    }

    public HashSet<EntityUid> GetEntitiesIntersecting(EntityUid gridId, Box2 worldAABB, LookupFlags flags = DefaultFlags)
    {
        var intersecting = new HashSet<EntityUid>();

        if (!_mapManager.GridExists(gridId))
            return intersecting;

        var localAABB = _transform.GetInvWorldMatrix(gridId).TransformBox(worldAABB);
        AddEntitiesIntersecting(gridId, intersecting, localAABB, flags);
        AddContained(intersecting, flags);

        return intersecting;
    }

    public HashSet<EntityUid> GetEntitiesIntersecting(EntityUid gridId, Box2Rotated worldBounds, LookupFlags flags = DefaultFlags)
    {
        var intersecting = new HashSet<EntityUid>();

        if (!_mapManager.GridExists(gridId))
            return intersecting;

        AddEntitiesIntersecting(gridId, intersecting, worldBounds, flags);
        AddContained(intersecting, flags);

        return intersecting;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerable<EntityUid> GetEntitiesIntersecting(TileRef tileRef, float enlargement = TileEnlargementRadius, LookupFlags flags = DefaultFlags)
    {
        return GetEntitiesIntersecting(tileRef.GridUid, tileRef.GridIndices, enlargement, flags);
    }

    #endregion

    #region Lookups

    /// <summary>
    /// Gets the relevant <see cref="BroadphaseComponent"/> that intersects the specified area.
    /// </summary>
    public void FindLookupsIntersecting(MapId mapId, Box2Rotated worldBounds, ComponentQueryCallback<BroadphaseComponent> callback)
    {
        if (mapId == MapId.Nullspace)
            return;

        var mapUid = _mapManager.GetMapEntityId(mapId);
        callback(mapUid, _broadQuery.GetComponent(mapUid));

        var state = (callback, _broadQuery);

        _mapManager.FindGridsIntersecting(mapId, worldBounds, ref state,
            static (EntityUid uid, MapGridComponent grid,
                ref (ComponentQueryCallback<BroadphaseComponent> callback, EntityQuery<BroadphaseComponent> _broadQuery)
                    tuple) =>
            {
                tuple.callback(uid, tuple._broadQuery.GetComponent(uid));
                return true;
            });
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
            var (_, wAng, wMat) = _transform.GetWorldPositionRotationMatrix(tileRef.GridUid);
            worldMatrix = wMat;
            angle = wAng;
        }

        var expand = new Vector2(0.5f, 0.5f);
        var center = worldMatrix.Value.Transform(tileRef.GridIndices + expand) * grid.TileSize;
        var translatedBox = Box2.CenteredAround(center, new Vector2(grid.TileSize, grid.TileSize));

        return new Box2Rotated(translatedBox, -angle.Value, center);
    }

    #endregion
}
