using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Shapes;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;

public sealed partial class EntityLookupSystem
{
    #region Private

    private void RecursiveAdd<T>(EntityUid uid, ref ValueList<Entity<T>> toAdd, EntityQuery<T> query) where T : IComponent
    {
        var xform = _xformQuery.GetComponent(uid);
        foreach (var child in xform._children)
        {
            if (query.TryGetComponent(child, out var compies))
            {
                toAdd.Add((child, compies));
            }

            RecursiveAdd(child, ref toAdd, query);
        }
    }

    private void AddContained<T>(HashSet<Entity<T>> intersecting, LookupFlags flags, EntityQuery<T> query) where T : IComponent
    {
        if ((flags & LookupFlags.Contained) == 0x0) return;

        var toAdd = new ValueList<Entity<T>>();

        foreach (var comp in intersecting)
        {
            if (!_containerQuery.TryGetComponent(comp, out var conManager)) continue;

            foreach (var con in _container.GetAllContainers(comp, conManager))
            {
                foreach (var contained in con.ContainedEntities)
                {
                    if (query.TryGetComponent(contained, out var compies))
                    {
                        toAdd.Add((contained, compies));
                    }

                    RecursiveAdd(contained, ref toAdd, query);
                }
            }
        }

        foreach (var uid in toAdd.Span)
        {
            intersecting.Add(uid);
        }
    }

    /// <summary>
    /// Common method to determine if an entity overlaps the specified shape.
    /// </summary>
    private bool IsIntersecting<TShape>(MapId mapId, EntityUid uid, TransformComponent xform, TShape shape, Transform shapeTransform, Box2 worldAABB, LookupFlags flags) where TShape : IPhysShape
    {
        var (entPos, entRot) = _transform.GetWorldPositionRotation(xform);

        if (xform.MapID != mapId ||
            !worldAABB.Contains(entPos) ||
            ((flags & LookupFlags.Contained) == 0x0 &&
             _container.IsEntityOrParentInContainer(uid, _metaQuery.GetComponent(uid), xform)))
        {
            return false;
        }

        if (_fixturesQuery.TryGetComponent(uid, out var fixtures))
        {
            var transform = new Transform(entPos, entRot);
            bool anyFixture = false;
            var sensors = (flags & LookupFlags.Sensors) != 0x0;

            foreach (var fixture in fixtures.Fixtures.Values)
            {
                if (!sensors && !fixture.Hard)
                    continue;

                anyFixture = true;
                for (var i = 0; i < fixture.Shape.ChildCount; i++)
                {
                    if (_manifoldManager.TestOverlap(shape, 0, fixture.Shape, i, shapeTransform, transform))
                    {
                        return true;
                    }
                }
            }

            if (anyFixture)
                return false;
        }

        if (!_fixtures.TestPoint(shape, shapeTransform, entPos))
            return false;

        return true;
    }

    private void AddLocalEntitiesIntersecting<T>(
        EntityUid lookupUid,
        HashSet<Entity<T>> intersecting,
        Box2 localAABB,
        LookupFlags flags,
        EntityQuery<T> query,
        BroadphaseComponent? lookup = null) where T : IComponent
    {
        if (!_broadQuery.Resolve(lookupUid, ref lookup))
            return;

        var polygon = new SlimPolygon(localAABB);
        AddEntitiesIntersecting(lookupUid, intersecting, polygon, localAABB, Physics.Transform.Empty, flags, query, lookup);
    }

    private void AddEntitiesIntersecting<T, TShape>(
        EntityUid lookupUid,
        HashSet<Entity<T>> intersecting,
        TShape shape,
        Box2 localAABB,
        Transform localTransform,
        LookupFlags flags,
        EntityQuery<T> query,
        BroadphaseComponent? lookup = null)
        where T : IComponent
       where TShape : IPhysShape
    {
        if (!_broadQuery.Resolve(lookupUid, ref lookup))
            return;

        var state = new QueryState<T, TShape>(
            intersecting,
            shape,
            localTransform,
            _fixtures,
            _physics,
            _manifoldManager,
            query,
            _fixturesQuery,
            (flags & LookupFlags.Sensors) != 0,
            (flags & LookupFlags.Approximate) != 0x0
        );

        if ((flags & LookupFlags.Dynamic) != 0x0)
        {
            lookup.DynamicTree.QueryAabb(ref state, PhysicsQuery, localAABB, true);
        }

        if ((flags & LookupFlags.Static) != 0x0)
        {
            lookup.StaticTree.QueryAabb(ref state, PhysicsQuery, localAABB, true);
        }

        if ((flags & LookupFlags.StaticSundries) == LookupFlags.StaticSundries)
        {
            lookup.StaticSundriesTree.QueryAabb(ref state, SundriesQuery, localAABB, true);
        }

        if ((flags & LookupFlags.Sundries) != 0x0)
        {
            lookup.SundriesTree.QueryAabb(ref state, SundriesQuery, localAABB, true);
        }

        return;

        static bool PhysicsQuery(ref QueryState<T, TShape> state, in FixtureProxy value)
        {
            if (!state.Sensors && !value.Fixture.Hard)
                return true;

            if (!state.Query.TryGetComponent(value.Entity, out var comp))
                return true;

            if (!state.Approximate)
            {
                var intersectingTransform = state.Physics.GetLocalPhysicsTransform(value.Entity);
                if (!state.Manifolds.TestOverlap(state.Shape, 0, value.Fixture.Shape, value.ChildIndex, state.Transform, intersectingTransform))
                {
                    return true;
                }
            }

            state.Intersecting.Add((value.Entity, comp));
            return true;
        }

        static bool SundriesQuery(ref QueryState<T, TShape> state, in EntityUid value)
        {
            if (!state.Query.TryGetComponent(value, out var comp))
                return true;

            if (state.Approximate)
            {
                state.Intersecting.Add((value, comp));
                return true;
            }

            var intersectingTransform = state.Physics.GetLocalPhysicsTransform(value);

            if (state.FixturesQuery.TryGetComponent(value, out var fixtures))
            {
                bool anyFixture = false;
                foreach (var fixture in fixtures.Fixtures.Values)
                {
                    if (!state.Sensors && !fixture.Hard)
                        continue;

                    anyFixture = true;
                    for (var i = 0; i < fixture.Shape.ChildCount; i++)
                    {
                        if (state.Manifolds.TestOverlap(state.Shape, 0, fixture.Shape, i, state.Transform,
                                intersectingTransform))
                        {
                            state.Intersecting.Add((value, comp));
                            return true;
                        }
                    }
                }

                if (anyFixture)
                    return true;
            }

            if (state.Fixtures.TestPoint(state.Shape, state.Transform, intersectingTransform.Position))
                state.Intersecting.Add((value, comp));

            return true;
        }
    }

    private bool AnyLocalComponentsIntersecting<T>(
        EntityUid lookupUid,
        Box2 localAABB,
        LookupFlags flags,
        EntityQuery<T> query,
        EntityUid? ignored = null,
        BroadphaseComponent? lookup = null) where T : IComponent
    {
        if (!_broadQuery.Resolve(lookupUid, ref lookup))
            return false;

        var polygon = new SlimPolygon(localAABB);
        var (lookupPos, lookupRot) = _transform.GetWorldPositionRotation(lookupUid);
        var transform = new Transform(lookupPos, lookupRot);
        var result = AnyComponentsIntersecting(lookupUid, polygon, localAABB, transform, flags, query, ignored, lookup);

        return result;
    }

    private bool AnyComponentsIntersecting<T, TShape>(
        EntityUid lookupUid,
        TShape shape,
        Box2 localAABB,
        Transform shapeTransform,
        LookupFlags flags,
        EntityQuery<T> query,
        EntityUid? ignored = null,
        BroadphaseComponent? lookup = null)
        where T : IComponent
       where TShape : IPhysShape
    {
        /*
         * Unfortunately this is split from the other query as we can short-circuit here, hence the code duplication.
         * Alternatively you can make them both use callbacks.
         */

        if (!_broadQuery.Resolve(lookupUid, ref lookup))
            return false;

        var state = new AnyQueryState<T, TShape>(false,
            ignored,
            shape,
            shapeTransform,
            _fixtures,
            _physics,
            _manifoldManager,
            query,
            _fixturesQuery,
            flags);

        if ((flags & LookupFlags.Dynamic) != 0x0)
        {
            lookup.DynamicTree.QueryAabb(ref state, PhysicsQuery, localAABB, true);

            if (state.Found)
                return true;
        }

        if ((flags & LookupFlags.Static) != 0x0)
        {
            lookup.StaticTree.QueryAabb(ref state, PhysicsQuery, localAABB, true);

            if (state.Found)
                return true;
        }

        if ((flags & LookupFlags.StaticSundries) == LookupFlags.StaticSundries)
        {
            lookup.StaticSundriesTree.QueryAabb(ref state, SundriesQuery, localAABB, true);

            if (state.Found)
                return true;
        }

        if ((flags & LookupFlags.Sundries) != 0x0)
        {
            lookup.SundriesTree.QueryAabb(ref state, SundriesQuery, localAABB, true);
        }

        return state.Found;

        static bool PhysicsQuery(ref AnyQueryState<T, TShape> state, in FixtureProxy value)
        {
            if (value.Entity == state.Ignored)
                return true;

            if (!state.Query.HasComponent(value.Entity))
                return true;

            var approx = (state.Flags & LookupFlags.Approximate) != 0x0;

            if (!approx)
            {
                var intersectingTransform = state.Physics.GetPhysicsTransform(value.Entity);

                if (!state.Manifolds.TestOverlap(state.Shape, 0, value.Fixture.Shape, value.ChildIndex,
                        state.Transform, intersectingTransform))
                {
                    return true;
                }
            }

            state.Found = true;
            return false;
        }

        static bool SundriesQuery(ref AnyQueryState<T, TShape> state, in EntityUid value)
        {
            if (state.Ignored == value)
                return true;

            if (!state.Query.HasComponent(value))
                return true;

            var approx = (state.Flags & LookupFlags.Approximate) != 0x0;

            if (approx)
            {
                state.Found = true;
                return false;
            }

            var intersectingTransform = state.Physics.GetPhysicsTransform(value);

            if (state.FixturesQuery.TryGetComponent(value, out var fixtures))
            {
                var sensors = (state.Flags & LookupFlags.Sensors) != 0x0;
                bool anyFixture = false;
                foreach (var fixture in fixtures.Fixtures.Values)
                {
                    if (!sensors && !fixture.Hard)
                        continue;

                    anyFixture = true;
                    for (var i = 0; i < fixture.Shape.ChildCount; i++)
                    {
                        if (state.Manifolds.TestOverlap(state.Shape, 0, fixture.Shape, i, state.Transform,
                                intersectingTransform))
                        {
                            state.Found = true;
                            return false;
                        }
                    }
                }

                if (anyFixture)
                    return true;
            }

            if (state.Fixtures.TestPoint(state.Shape, state.Transform, intersectingTransform.Position))
            {
                state.Found = true;
                return false;
            }

            return true;
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
    private bool UseBoundsQuery<T>(float area) where T : IComponent
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
        var polygon = new SlimPolygon(worldAABB);
        var transform = Physics.Transform.Empty;
        var result = AnyComponentsIntersecting(type, mapId, polygon, transform, ignored, flags);
        return result;
    }

    public bool AnyComponentsIntersecting<T>(Type type, MapId mapId, T shape, Transform shapeTransform, EntityUid? ignored = null, LookupFlags flags = DefaultFlags) where T : IPhysShape
    {
        DebugTools.Assert(typeof(IComponent).IsAssignableFrom(type));
        if (mapId == MapId.Nullspace)
            return false;

        var worldAABB = shape.ComputeAABB(shapeTransform, 0);

        if (!UseBoundsQuery(type, worldAABB.Height * worldAABB.Width))
        {
            foreach (var (uid, comp) in EntityManager.GetAllComponents(type, true))
            {
                var xform = _xformQuery.GetComponent(uid);

                if (!IsIntersecting(mapId, uid, xform, shape, shapeTransform, worldAABB, flags))
                    continue;

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
                    EntityQuery<IComponent> query,
                    EntityUid? ignored,
                    bool found) tuple) =>
                {
                    if (!tuple.system.AnyLocalComponentsIntersecting(uid, tuple.worldAABB, tuple.flags, tuple.query, tuple.ignored))
                        return true;

                    tuple.found = true;
                    return false;
                }, approx: true, includeMap: false);

            if (state.found)
            {
                return true;
            }

            // Get map entities
            var mapUid = _map.GetMapOrInvalid(mapId);
            AnyLocalComponentsIntersecting(mapUid, worldAABB, flags, query, ignored);
        }

        return false;
    }

    public void GetEntitiesIntersecting(Type type, MapId mapId, Box2 worldAABB, HashSet<Entity<IComponent>> intersecting, LookupFlags flags = DefaultFlags)
    {
        DebugTools.Assert(typeof(IComponent).IsAssignableFrom(type));
        if (mapId == MapId.Nullspace)
            return;

        var polygon = new SlimPolygon(worldAABB);
        var transform = Physics.Transform.Empty;

        GetEntitiesIntersecting(type, mapId, polygon, transform, intersecting, flags);
    }

    public void GetEntitiesIntersecting<T>(MapId mapId, Box2Rotated worldBounds, HashSet<Entity<T>> entities, LookupFlags flags = DefaultFlags) where T : IComponent
    {
        if (mapId == MapId.Nullspace) return;

        var polygon = new SlimPolygon(worldBounds);
        var shapeTransform = Physics.Transform.Empty;

        GetEntitiesIntersecting(mapId, polygon, shapeTransform, entities, flags);
    }

    public void GetEntitiesIntersecting<T>(MapId mapId, Box2 worldAABB, HashSet<Entity<T>> entities, LookupFlags flags = DefaultFlags) where T : IComponent
    {
        if (mapId == MapId.Nullspace) return;

        var polygon = new SlimPolygon(worldAABB);
        var shapeTransform = Physics.Transform.Empty;

        GetEntitiesIntersecting(mapId, polygon, shapeTransform, entities, flags);
    }

    #endregion

    #region IPhysShape

    public void GetEntitiesIntersecting<T>(Type type, MapId mapId, T shape, Transform shapeTransform, HashSet<Entity<IComponent>> intersecting, LookupFlags flags = DefaultFlags) where T : IPhysShape
    {
        DebugTools.Assert(typeof(IComponent).IsAssignableFrom(type));
        if (mapId == MapId.Nullspace)
            return;

        var worldAABB = shape.ComputeAABB(shapeTransform, 0);

        if (!UseBoundsQuery(type, worldAABB.Height * worldAABB.Width))
        {
            foreach (var (uid, comp) in EntityManager.GetAllComponents(type, true))
            {
                var xform = _xformQuery.GetComponent(uid);

                if (!IsIntersecting(mapId, uid, xform, shape, shapeTransform, worldAABB, flags))
                    continue;

                intersecting.Add((uid, comp));
            }
        }
        else
        {
            var query = EntityManager.GetEntityQuery(type);

            // Get grid entities
            var state = new GridQueryState<IComponent, T>(intersecting, shape, shapeTransform, this, _physics, flags, query);

            _mapManager.FindGridsIntersecting(mapId, worldAABB, ref state,
                static (EntityUid uid, MapGridComponent grid, ref GridQueryState<IComponent, T> state) =>
                {
                    var localTransform = state.Physics.GetRelativePhysicsTransform(state.Transform, uid);
                    var localAabb = state.Shape.ComputeAABB(localTransform, 0);
                    state.Lookup.AddEntitiesIntersecting(uid, state.Intersecting, state.Shape, localAabb, localTransform, state.Flags, state.Query);
                    return true;
                }, approx: true, includeMap: false);

            var mapUid = _map.GetMapOrInvalid(mapId);
            var localTransform = state.Physics.GetRelativePhysicsTransform(state.Transform, mapUid);
            var localAabb = state.Shape.ComputeAABB(localTransform, 0);

            AddEntitiesIntersecting(mapUid, intersecting, shape, localAabb, localTransform, flags, query);

            AddContained(intersecting, flags, query);
        }
    }

    public void GetEntitiesIntersecting<T, TShape>(MapId mapId, TShape shape, Transform shapeTransform, HashSet<Entity<T>> entities, LookupFlags flags = DefaultFlags)
        where T : IComponent
        where TShape : IPhysShape
    {
        if (mapId == MapId.Nullspace) return;

        var worldAABB = shape.ComputeAABB(shapeTransform, 0);

        if (!UseBoundsQuery<T>(worldAABB.Height * worldAABB.Width))
        {
            var query = AllEntityQuery<T, TransformComponent>();

            while (query.MoveNext(out var uid, out var comp, out var xform))
            {
                if (!IsIntersecting(mapId, uid, xform, shape, shapeTransform, worldAABB, flags))
                    continue;

                entities.Add((uid, comp));
            }
        }
        else
        {
            var query = GetEntityQuery<T>();

            // Get grid entities
            var state = new GridQueryState<T, TShape>(entities, shape, shapeTransform, this, _physics, flags, query);

            _mapManager.FindGridsIntersecting(mapId, worldAABB, ref state,
                static (EntityUid uid, MapGridComponent grid, ref GridQueryState<T, TShape> state) =>
                {
                    var localTransform = state.Physics.GetRelativePhysicsTransform(state.Transform, uid);
                    var localAabb = state.Shape.ComputeAABB(localTransform, 0);
                    state.Lookup.AddEntitiesIntersecting(uid, state.Intersecting, state.Shape, localAabb, localTransform, state.Flags, state.Query);
                    return true;
                }, approx: true, includeMap: false);

            // Get map entities
            var mapUid = _map.GetMapOrInvalid(mapId);
            var localTransform = state.Physics.GetRelativePhysicsTransform(state.Transform, mapUid);
            var localAabb = state.Shape.ComputeAABB(localTransform, 0);

            AddEntitiesIntersecting(mapUid, entities, shape, localAabb, localTransform, flags, query);
            AddContained(entities, flags, query);
        }
    }

    #endregion

    #region EntityCoordinates

    public void GetEntitiesInRange<T>(EntityCoordinates coordinates, float range, HashSet<Entity<T>> entities, LookupFlags flags = DefaultFlags) where T : IComponent
    {
        var mapPos = _transform.ToMapCoordinates(coordinates);
        GetEntitiesInRange(mapPos, range, entities, flags);
    }

    public HashSet<Entity<T>> GetEntitiesInRange<T>(EntityCoordinates coordinates, float range, LookupFlags flags = DefaultFlags) where T : IComponent
    {
        var entities = new HashSet<Entity<T>>();
        GetEntitiesInRange(coordinates, range, entities, flags);
        return entities;
    }

    #endregion

    #region MapCoordinates

    public HashSet<Entity<IComponent>> GetEntitiesInRange(Type type, MapCoordinates coordinates, float range)
    {
        var entities = new HashSet<Entity<IComponent>>();
        GetEntitiesInRange(type, coordinates, range, entities);
        return entities;
    }

    public void GetEntitiesInRange(Type type, MapCoordinates coordinates, float range, HashSet<Entity<IComponent>> entities, LookupFlags flags = DefaultFlags)
    {
        DebugTools.Assert(typeof(IComponent).IsAssignableFrom(type));
        GetEntitiesInRange(type, coordinates.MapId, coordinates.Position, range, entities, flags);
    }

    public void GetEntitiesInRange<T>(MapCoordinates coordinates, float range, HashSet<Entity<T>> entities, LookupFlags flags = DefaultFlags) where T : IComponent
    {
        GetEntitiesInRange(coordinates.MapId, coordinates.Position, range, entities, flags);
    }

    public HashSet<Entity<T>> GetEntitiesInRange<T>(MapCoordinates coordinates, float range, LookupFlags flags = DefaultFlags) where T : IComponent
    {
        var entities = new HashSet<Entity<T>>();
        GetEntitiesInRange(coordinates.MapId, coordinates.Position, range, entities, flags);
        return entities;
    }

    #endregion

    #region MapId

    public void GetEntitiesInRange(Type type, MapId mapId, Vector2 worldPos, float range, HashSet<Entity<IComponent>> entities, LookupFlags flags = DefaultFlags)
    {
        DebugTools.Assert(typeof(IComponent).IsAssignableFrom(type));
        DebugTools.Assert(range > 0, "Range must be a positive float");

        if (mapId == MapId.Nullspace) return;

        var circle = new PhysShapeCircle(range);
        var transform = new Transform(worldPos, 0f);
        GetEntitiesIntersecting(type, mapId, circle, transform, entities, flags);
    }

    public void GetEntitiesInRange<T>(MapId mapId, Vector2 worldPos, float range, HashSet<Entity<T>> entities, LookupFlags flags = DefaultFlags) where T : IComponent
    {
        var shape = new PhysShapeCircle(range, worldPos);
        var transform = Physics.Transform.Empty;

        GetEntitiesInRange(mapId, shape, transform, entities, flags);
    }

    public void GetEntitiesInRange<T, TShape>(MapId mapId, TShape shape, Transform transform, HashSet<Entity<T>> entities, LookupFlags flags = DefaultFlags)
        where T : IComponent
        where TShape : IPhysShape
    {
        DebugTools.Assert(shape.Radius > 0, "Range must be a positive float");

        if (mapId == MapId.Nullspace) return;

        GetEntitiesIntersecting(mapId, shape, transform, entities, flags);
    }

    /// <summary>
    /// Gets entities with the specified component with the specified map.
    /// </summary>
    public void GetEntitiesOnMap<TComp1>(MapId mapId, HashSet<Entity<TComp1>> entities) where TComp1 : IComponent
    {
        var query = AllEntityQuery<TComp1, TransformComponent>();

        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (xform.MapID != mapId)
                continue;

            entities.Add((uid, comp));
        }
    }

    /// <summary>
    /// Gets entities with the specified component with the specified parent.
    /// </summary>
    public void GetEntitiesOnMap<TComp1, TComp2>(MapId mapId, HashSet<Entity<TComp1, TComp2>> entities)
        where TComp1 : IComponent
        where TComp2 : IComponent
    {
        var query = AllEntityQuery<TComp1, TComp2, TransformComponent>();

        while (query.MoveNext(out var uid, out var comp, out var comp2, out var xform))
        {
            if (xform.MapID != mapId)
                continue;

            entities.Add((uid, comp, comp2));
        }
    }

    #endregion

    #region Local

    /// <summary>
    /// Gets the entities intersecting the specified broadphase entity using a local AABB.
    /// </summary>
    public void GetLocalEntitiesIntersecting<T>(
        EntityUid gridUid,
        Vector2i localTile,
        HashSet<Entity<T>> intersecting,
        float enlargement = TileEnlargementRadius,
        LookupFlags flags = DefaultFlags,
        MapGridComponent? gridComp = null) where T : IComponent
    {
        ushort tileSize = 1;

        if (_gridQuery.Resolve(gridUid, ref gridComp))
        {
            tileSize = gridComp.TileSize;
        }

        var localAABB = GetLocalBounds(localTile, tileSize);
        localAABB = localAABB.Enlarged(enlargement);
        GetLocalEntitiesIntersecting(gridUid, localAABB, intersecting, flags);
    }

    /// <summary>
    /// Gets the entities intersecting the specified broadphase entity using a local AABB.
    /// </summary>
    public void GetLocalEntitiesIntersecting<T>(
        EntityUid gridUid,
        Box2 localAABB,
        HashSet<Entity<T>> intersecting,
        LookupFlags flags = DefaultFlags) where T : IComponent
    {
        var query = GetEntityQuery<T>();
        AddLocalEntitiesIntersecting(gridUid, intersecting, localAABB, flags, query);
        AddContained(intersecting, flags, query);
    }

    /// <summary>
    /// Gets the entities intersecting the specified broadphase entity using a local AABB.
    /// </summary>
    public void GetLocalEntitiesIntersecting<T>(
        Entity<BroadphaseComponent> grid,
        Box2 localAABB,
        HashSet<Entity<T>> intersecting,
        EntityQuery<T> query,
        LookupFlags flags = DefaultFlags) where T : IComponent
    {
        AddLocalEntitiesIntersecting(grid, intersecting, localAABB, flags, query, grid.Comp);
        AddContained(intersecting, flags, query);
    }

    #endregion

    /// <summary>
    /// Gets entities with the specified component with the specified grid.
    /// </summary>
    public void GetGridEntities<TComp1>(EntityUid gridUid, HashSet<Entity<TComp1>> entities) where TComp1 : IComponent
    {
        var query = AllEntityQuery<TComp1, TransformComponent>();

        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (xform.GridUid != gridUid)
                continue;

            entities.Add((uid, comp));
        }
    }

    /// <summary>
    /// Gets entities with the specified component with the specified parent.
    /// </summary>
    public void GetChildEntities<TComp1>(EntityUid parentUid, HashSet<Entity<TComp1>> entities) where TComp1 : IComponent
    {
        var query = AllEntityQuery<TComp1, TransformComponent>();

        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (xform.ParentUid != parentUid)
                continue;

            entities.Add((uid, comp));
        }
    }

    /// <summary>
    /// Gets entities with the specified component with the specified parent.
    /// </summary>
    public void GetChildEntities<TComp1, TComp2>(EntityUid parentUid, HashSet<Entity<TComp1, TComp2>> entities)
        where TComp1 : IComponent
        where TComp2 : IComponent
    {
        var query = AllEntityQuery<TComp1, TComp2, TransformComponent>();

        while (query.MoveNext(out var uid, out var comp, out var comp2, out var xform))
        {
            if (xform.ParentUid != parentUid)
                continue;

            entities.Add((uid, comp, comp2));
        }
    }

    private readonly record struct GridQueryState<T, TShape>(
        HashSet<Entity<T>> Intersecting,
        TShape Shape,
        Transform Transform,
        EntityLookupSystem Lookup,
        SharedPhysicsSystem Physics,
        LookupFlags Flags,
        EntityQuery<T> Query
    ) where T : IComponent
        where TShape : IPhysShape;

    private record struct AnyQueryState<T, TShape>(
        bool Found,
        EntityUid? Ignored,
        TShape Shape,
        Transform Transform,
        FixtureSystem Fixtures,
        SharedPhysicsSystem Physics,
        IManifoldManager Manifolds,
        EntityQuery<T> Query,
        EntityQuery<FixturesComponent> FixturesQuery,
        LookupFlags Flags
    ) where T : IComponent
     where TShape : IPhysShape;

    private readonly record struct QueryState<T, TShape>(
        HashSet<Entity<T>> Intersecting,
        TShape Shape,
        Transform Transform,
        FixtureSystem Fixtures,
        SharedPhysicsSystem Physics,
        IManifoldManager Manifolds,
        EntityQuery<T> Query,
        EntityQuery<FixturesComponent> FixturesQuery,
        bool Sensors,
        bool Approximate
    ) where T : IComponent
     where TShape : IPhysShape;
}
