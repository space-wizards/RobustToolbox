using System.Collections.Generic;
using Robust.Shared.Containers;
using Robust.Shared.Map;
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
        EntityQuery<BroadphaseComponent> lookupQuery,
        EntityQuery<TransformComponent> xformQuery,
        EntityQuery<T> query) where T : Component
    {
        var lookup = lookupQuery.GetComponent(lookupUid);
        var invMatrix = _transform.GetInvWorldMatrix(lookupUid, xformQuery);
        var localAABB = invMatrix.TransformBox(worldAABB);
        var state = (intersecting, query);

        if ((flags & LookupFlags.Dynamic) != 0x0)
        {
            lookup.DynamicTree.QueryAabb(ref state, static (ref (HashSet<T> intersecting, EntityQuery<T> query) tuple, in FixtureProxy value) =>
            {
                if (!tuple.query.TryGetComponent(value.Fixture.Body.Owner, out var comp))
                    return true;

                tuple.intersecting.Add(comp);
                return true;
            }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        if ((flags & (LookupFlags.Static | LookupFlags.Anchored)) != 0x0)
        {
            lookup.StaticTree.QueryAabb(ref state, static (ref (HashSet<T> intersecting, EntityQuery<T> query) tuple, in FixtureProxy value) =>
            {
                if (!tuple.query.TryGetComponent(value.Fixture.Body.Owner, out var comp))
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

    private void RecursiveAdd<T>(EntityUid uid, List<T> toAdd, EntityQuery<TransformComponent> xformQuery, EntityQuery<T> query) where T : Component
    {
        var childEnumerator = xformQuery.GetComponent(uid).ChildEnumerator;

        while (childEnumerator.MoveNext(out var child))
        {
            if (query.TryGetComponent(child.Value, out var compies))
            {
                toAdd.Add(compies);
            }

            RecursiveAdd(child.Value, toAdd, xformQuery, query);
        }
    }

    private void AddContained<T>(HashSet<T> intersecting, LookupFlags flags, EntityQuery<TransformComponent> xformQuery, EntityQuery<T> query) where T : Component
    {
        if ((flags & LookupFlags.Contained) == 0x0) return;

        var conQuery = GetEntityQuery<ContainerManagerComponent>();
        var toAdd = new List<T>();

        foreach (var comp in intersecting)
        {
            if (!conQuery.TryGetComponent(comp.Owner, out var conManager)) continue;

            foreach (var con in conManager.GetAllContainers())
            {
                foreach (var contained in con.ContainedEntities)
                {
                    if (query.TryGetComponent(contained, out var compies))
                    {
                        toAdd.Add(compies);
                    }

                    RecursiveAdd(contained, toAdd, xformQuery, query);
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

    public HashSet<T> GetComponentsIntersecting<T>(MapId mapId, Box2 worldAABB, LookupFlags flags = DefaultFlags) where T : Component
    {
        if (mapId == MapId.Nullspace) return new HashSet<T>();

        var xformQuery = GetEntityQuery<TransformComponent>();
        var intersecting = new HashSet<T>();

        if (!UseBoundsQuery<T>(worldAABB.Height * worldAABB.Width))
        {
            foreach (var (comp, xform) in EntityQuery<T, TransformComponent>(true))
            {
                if (xform.MapID != mapId || !worldAABB.Contains(_transform.GetWorldPosition(comp.Owner, xformQuery))) continue;
                intersecting.Add(comp);
            }
        }
        else
        {
            var query = GetEntityQuery<T>();
            var lookupQuery = GetEntityQuery<BroadphaseComponent>();
            // Get grid entities
            foreach (var grid in _mapManager.FindGridsIntersecting(mapId, worldAABB))
            {
                AddComponentsIntersecting(grid.GridEntityId, intersecting, worldAABB, flags, lookupQuery, xformQuery, query);
            }

            // Get map entities
            var mapUid = _mapManager.GetMapEntityId(mapId);
            AddComponentsIntersecting(mapUid, intersecting, worldAABB, flags, lookupQuery, xformQuery, query);
            AddContained(intersecting, flags, xformQuery, query);
        }

        return intersecting;
    }

    #endregion

    #region EntityCoordinates

    public HashSet<T> GetComponentsInRange<T>(EntityCoordinates coordinates, float range) where T : Component
    {
        var mapPos = coordinates.ToMap(EntityManager);
        return GetComponentsInRange<T>(mapPos, range);
    }
    #endregion

    #region MapCoordinates

    public HashSet<T> GetComponentsInRange<T>(MapCoordinates coordinates, float range) where T : Component
    {
        return GetComponentsInRange<T>(coordinates.MapId, coordinates.Position, range);
    }

    #endregion

    #region MapId

    public HashSet<T> GetComponentsInRange<T>(MapId mapId, Vector2 worldPos, float range) where T : Component
    {
        DebugTools.Assert(range > 0, "Range must be a positive float");

        if (mapId == MapId.Nullspace) return new HashSet<T>();

        // TODO: Actual circles
        var worldAABB = new Box2(worldPos - range, worldPos + range);
        return GetComponentsIntersecting<T>(mapId, worldAABB);
    }

    #endregion
}
