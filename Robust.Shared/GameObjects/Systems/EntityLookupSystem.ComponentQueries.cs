using System;
using System.Collections.Generic;
using System.Numerics;
using Robust.Shared.Collections;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;

public sealed partial class EntityLookupSystem
{
    #region Private

    private void AddComponentsIntersecting<T>(
        EntityUid lookupUid,
        HashSet<T> intersecting,
        Box2 worldAABB,
        LookupFlags flags,
        EntityQuery<T> query) where T : Component
    {
        var lookup = _broadQuery.GetComponent(lookupUid);
        var invMatrix = _transform.GetInvWorldMatrix(lookupUid);
        var localAABB = invMatrix.TransformBox(worldAABB);
        var state = (intersecting, query);

        if ((flags & LookupFlags.Dynamic) != 0x0)
        {
            lookup.DynamicTree.QueryAabb(ref state, static (ref (HashSet<T> intersecting, EntityQuery<T> query) tuple, in FixtureProxy value) =>
            {
                if (!tuple.query.TryGetComponent(value.Entity, out var comp))
                    return true;

                tuple.intersecting.Add(comp);
                return true;
            }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        if ((flags & (LookupFlags.Static)) != 0x0)
        {
            lookup.StaticTree.QueryAabb(ref state, static (ref (HashSet<T> intersecting, EntityQuery<T> query) tuple, in FixtureProxy value) =>
            {
                if (!tuple.query.TryGetComponent(value.Entity, out var comp))
                    return true;

                tuple.intersecting.Add(comp);
                return true;
            }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        if ((flags & LookupFlags.StaticSundries) == LookupFlags.StaticSundries)
        {
            lookup.StaticSundriesTree.QueryAabb(ref state, static (ref (HashSet<T> intersecting, EntityQuery<T> query) tuple, in EntityUid value) =>
            {
                if (!tuple.query.TryGetComponent(value, out var comp))
                    return true;

                tuple.intersecting.Add(comp);
                return true;
            }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        if ((flags & LookupFlags.Sundries) != 0x0)
        {
            lookup.SundriesTree.QueryAabb(ref state, static (ref (HashSet<T> intersecting, EntityQuery<T> query) tuple, in EntityUid value) =>
            {
                if (!tuple.query.TryGetComponent(value, out var comp))
                    return true;

                tuple.intersecting.Add(comp);
                return true;
            }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }
    }

    private bool AnyComponentsIntersecting<T>(
        EntityUid lookupUid,
        Box2 worldAABB,
        LookupFlags flags,
        EntityQuery<T> query,
        EntityUid? ignored = null) where T : Component
    {
        var lookup = _broadQuery.GetComponent(lookupUid);
        var invMatrix = _transform.GetInvWorldMatrix(lookupUid);
        var localAABB = invMatrix.TransformBox(worldAABB);
        var state = (query, ignored, found: false);

        if ((flags & LookupFlags.Dynamic) != 0x0)
        {
            lookup.DynamicTree.QueryAabb(ref state,
                static (ref (EntityQuery<T> query, EntityUid? ignored, bool found) tuple, in FixtureProxy value) =>
                {
                    if (value.Entity == tuple.ignored)
                        return true;

                    tuple.found = true;
                    return false;
                }, localAABB, (flags & LookupFlags.Approximate) != 0x0);

            if (state.found)
                return true;
        }

        if ((flags & LookupFlags.Static) != 0x0)
        {
            lookup.StaticTree.QueryAabb(ref state,
                static (ref (EntityQuery<T> query, EntityUid? ignored, bool found) tuple, in FixtureProxy value) =>
                {
                    if (value.Entity == tuple.ignored)
                        return true;

                    tuple.found = true;
                    return false;
                }, localAABB, (flags & LookupFlags.Approximate) != 0x0);

            if (state.found)
                return true;
        }

        if ((flags & LookupFlags.StaticSundries) == LookupFlags.StaticSundries)
        {
            lookup.StaticSundriesTree.QueryAabb(ref state,
                static (ref (EntityQuery<T> query, EntityUid? ignored, bool found) tuple, in EntityUid value) =>
                {
                    if (value == tuple.ignored)
                        return true;

                    tuple.found = true;
                    return false;
                }, localAABB, (flags & LookupFlags.Approximate) != 0x0);

            if (state.found)
                return true;
        }

        if ((flags & LookupFlags.Sundries) != 0x0)
        {
            lookup.SundriesTree.QueryAabb(ref state,
                static (ref (EntityQuery<T> query, EntityUid? ignored, bool found) tuple, in EntityUid value) =>
                {
                    if (value == tuple.ignored)
                        return true;

                    tuple.found = true;
                    return false;
                }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        return state.found;
    }

    private void RecursiveAdd<T>(EntityUid uid, ref ValueList<T> toAdd, EntityQuery<T> query) where T : Component
    {
        var childEnumerator = _xformQuery.GetComponent(uid).ChildEnumerator;

        while (childEnumerator.MoveNext(out var child))
        {
            if (query.TryGetComponent(child.Value, out var compies))
            {
                toAdd.Add(compies);
            }

            RecursiveAdd(child.Value, ref toAdd, query);
        }
    }

    private void AddContained<T>(HashSet<T> intersecting, LookupFlags flags, EntityQuery<T> query) where T : Component
    {
        if ((flags & LookupFlags.Contained) == 0x0) return;

        var toAdd = new ValueList<T>();

        foreach (var comp in intersecting)
        {
            if (!_containerQuery.TryGetComponent(comp.Owner, out var conManager)) continue;

            foreach (var con in conManager.GetAllContainers())
            {
                foreach (var contained in con.ContainedEntities)
                {
                    if (query.TryGetComponent(contained, out var compies))
                    {
                        toAdd.Add(compies);
                    }

                    RecursiveAdd(contained, ref toAdd, query);
                }
            }
        }

        foreach (var uid in toAdd)
        {
            intersecting.Add(uid);
        }
    }

    /// <summary>
    /// Should we just iterate every component and check position or do bounds checks.
    /// </summary>
    private bool UseBoundsQuery(Type type, float area)
    {
        return Count(type) > area;
    }

    /// <summary>
    /// Should we just iterate every component and check position or do bounds checks.
    /// </summary>
    private bool UseBoundsQuery<T>(float area) where T : Component
    {
        // If the component has a low count we'll just do an estimate if it's faster to iterate every comp directly
        // Might be useful to have some way to expose this to content?
        // For now we'll assume 1 entity per metre.
        return Count<T>() > area;
    }

    #endregion

    // Like .Queries but works with components
    #region Box2

    public bool AnyComponentsIntersecting(Type type, MapId mapId, Box2 worldAABB, EntityUid? ignored = null, LookupFlags flags = DefaultFlags)
    {
        DebugTools.Assert(typeof(Component).IsAssignableFrom(type));
        if (mapId == MapId.Nullspace) return false;

        if (!UseBoundsQuery(type, worldAABB.Height * worldAABB.Width))
        {
            foreach (var (uid, comp) in EntityManager.GetAllComponents(type, true))
            {
                var xform = _xformQuery.GetComponent(uid);

                if (xform.MapID != mapId ||
                    !worldAABB.Contains(_transform.GetWorldPosition(xform)) ||
                    ((flags & LookupFlags.Contained) == 0x0 &&
                    _container.IsEntityOrParentInContainer(uid, _metaQuery.GetComponent(uid), xform, _metaQuery, _xformQuery)))
                {
                    continue;
                }

                return true;
            }
        }
        else
        {
            var query = EntityManager.GetEntityQuery(type);

            // Get grid entities
            var state = (this, worldAABB, flags, query, ignored, found: false);

            _mapManager.FindGridsIntersecting(mapId, worldAABB, ref state,
                static (EntityUid uid, MapGridComponent grid, ref
                    (EntityLookupSystem system,
                    Box2 worldAABB,
                    LookupFlags flags,
                    EntityQuery<Component> query,
                    EntityUid? ignored,
                    bool found) tuple) =>
                {
                    if (!tuple.system.AnyComponentsIntersecting(uid, tuple.worldAABB, tuple.flags, tuple.query, tuple.ignored))
                        return true;
                    tuple.found = true;
                    return false;
                }, (flags & LookupFlags.Approximate) != 0x0);

            // Get map entities
            var mapUid = _mapManager.GetMapEntityId(mapId);
            AnyComponentsIntersecting(mapUid, worldAABB, flags, query, ignored);
        }

        return false;
    }

    public HashSet<Component> GetComponentsIntersecting(Type type, MapId mapId, Box2 worldAABB, LookupFlags flags = DefaultFlags)
    {
        DebugTools.Assert(typeof(Component).IsAssignableFrom(type));
        if (mapId == MapId.Nullspace)
            return new HashSet<Component>();

        var intersecting = new HashSet<Component>();

        if (!UseBoundsQuery(type, worldAABB.Height * worldAABB.Width))
        {
            foreach (var (uid, comp) in EntityManager.GetAllComponents(type, true))
            {
                var xform = _xformQuery.GetComponent(uid);

                if (xform.MapID != mapId ||
                    !worldAABB.Contains(_transform.GetWorldPosition(xform)) ||
                    ((flags & LookupFlags.Contained) == 0x0 &&
                     _container.IsEntityOrParentInContainer(uid, _metaQuery.GetComponent(uid), xform, _metaQuery, _xformQuery)))
                {
                    continue;
                }

                intersecting.Add(comp);
            }
        }
        else
        {
            var query = EntityManager.GetEntityQuery(type);

            // Get grid entities
            var state = (this, worldAABB, flags, query, intersecting);

            _mapManager.FindGridsIntersecting(mapId, worldAABB, ref state,
                static (EntityUid uid, MapGridComponent grid,
                    ref (EntityLookupSystem system,
                        Box2 worldAABB,
                        LookupFlags flags,
                        EntityQuery<Component> query,
                        HashSet<Component> intersecting) tuple) =>
                {
                    tuple.system.AddComponentsIntersecting(uid, tuple.intersecting, tuple.worldAABB, tuple.flags, tuple.query);
                    return true;
                }, (flags & LookupFlags.Approximate) != 0x0);

            // Get map entities
            var mapUid = _mapManager.GetMapEntityId(mapId);
            AddComponentsIntersecting(mapUid, intersecting, worldAABB, flags, query);
            AddContained(intersecting, flags, query);
        }

        return intersecting;
    }

    public HashSet<T> GetComponentsIntersecting<T>(MapId mapId, Box2 worldAABB, LookupFlags flags = DefaultFlags) where T : Component
    {
        if (mapId == MapId.Nullspace) return new HashSet<T>();

        var intersecting = new HashSet<T>();

        if (!UseBoundsQuery<T>(worldAABB.Height * worldAABB.Width))
        {
            var query = AllEntityQuery<T, TransformComponent>();

            while (query.MoveNext(out var comp, out var xform))
            {
                if (xform.MapID != mapId || !worldAABB.Contains(_transform.GetWorldPosition(xform))) continue;
                intersecting.Add(comp);
            }
        }
        else
        {
            var query = GetEntityQuery<T>();

            // Get grid entities
            var state = (this, worldAABB, flags, query, intersecting);

            _mapManager.FindGridsIntersecting(mapId, worldAABB, ref state,
                static (EntityUid uid, MapGridComponent grid,
                ref (EntityLookupSystem system,
                    Box2 worldAABB,
                    LookupFlags flags,
                    EntityQuery<T> query,
                    HashSet<T> intersecting) tuple) =>
            {
                tuple.system.AddComponentsIntersecting(uid, tuple.intersecting, tuple.worldAABB, tuple.flags, tuple.query);
                return true;
            }, (flags & LookupFlags.Approximate) != 0x0);

            // Get map entities
            var mapUid = _mapManager.GetMapEntityId(mapId);
            AddComponentsIntersecting(mapUid, intersecting, worldAABB, flags, query);
            AddContained(intersecting, flags, query);
        }

        return intersecting;
    }

    #endregion

    #region EntityCoordinates

    public HashSet<T> GetComponentsInRange<T>(EntityCoordinates coordinates, float range) where T : Component
    {
        var mapPos = coordinates.ToMap(EntityManager, _transform);
        return GetComponentsInRange<T>(mapPos, range);
    }
    #endregion

    #region MapCoordinates

    public HashSet<Component> GetComponentsInRange(Type type, MapCoordinates coordinates, float range)
    {
        DebugTools.Assert(typeof(Component).IsAssignableFrom(type));
        return GetComponentsInRange(type, coordinates.MapId, coordinates.Position, range);
    }

    public HashSet<T> GetComponentsInRange<T>(MapCoordinates coordinates, float range) where T : Component
    {
        return GetComponentsInRange<T>(coordinates.MapId, coordinates.Position, range);
    }

    #endregion

    #region MapId

    public bool AnyComponentsInRange(Type type, MapId mapId, Vector2 worldPos, float range)
    {
        DebugTools.Assert(typeof(Component).IsAssignableFrom(type));
        DebugTools.Assert(range > 0, "Range must be a positive float");

        if (mapId == MapId.Nullspace) return false;

        // TODO: Actual circles
        var rangeVec = new Vector2(range, range);

        var worldAABB = new Box2(worldPos - rangeVec, worldPos + rangeVec);
        return AnyComponentsIntersecting(type, mapId, worldAABB);
    }

    public HashSet<Component> GetComponentsInRange(Type type, MapId mapId, Vector2 worldPos, float range)
    {
        DebugTools.Assert(typeof(Component).IsAssignableFrom(type));
        DebugTools.Assert(range > 0, "Range must be a positive float");

        if (mapId == MapId.Nullspace) return new HashSet<Component>();

        // TODO: Actual circles
        var rangeVec = new Vector2(range, range);

        var worldAABB = new Box2(worldPos - rangeVec, worldPos + rangeVec);
        return GetComponentsIntersecting(type, mapId, worldAABB);
    }

    public HashSet<T> GetComponentsInRange<T>(MapId mapId, Vector2 worldPos, float range) where T : Component
    {
        DebugTools.Assert(range > 0, "Range must be a positive float");

        if (mapId == MapId.Nullspace) return new HashSet<T>();

        // TODO: Actual circles
        var rangeVec = new Vector2(range, range);

        var worldAABB = new Box2(worldPos - rangeVec, worldPos + rangeVec);
        return GetComponentsIntersecting<T>(mapId, worldAABB);
    }

    #endregion
}
