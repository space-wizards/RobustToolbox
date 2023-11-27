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
using Robust.Shared.Physics.Systems;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;

public sealed partial class EntityLookupSystem
{
    #region Private

    private void AddEntitiesIntersecting<T>(
        EntityUid lookupUid,
        HashSet<Entity<T>> intersecting,
        Box2 worldAABB,
        LookupFlags flags,
        EntityQuery<T> query) where T : IComponent
    {
        var lookup = _broadQuery.GetComponent(lookupUid);
        var invMatrix = _transform.GetInvWorldMatrix(lookupUid);
        var localAABB = invMatrix.TransformBox(worldAABB);
        var state = (intersecting, query);

        if ((flags & LookupFlags.Dynamic) != 0x0)
        {
            lookup.DynamicTree.QueryAabb(ref state, static (ref (HashSet<Entity<T>> intersecting, EntityQuery<T> query) tuple, in FixtureProxy value) =>
            {
                if (!tuple.query.TryGetComponent(value.Entity, out var comp))
                    return true;

                tuple.intersecting.Add((value.Entity, comp));
                return true;
            }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        if ((flags & (LookupFlags.Static)) != 0x0)
        {
            lookup.StaticTree.QueryAabb(ref state, static (ref (HashSet<Entity<T>> intersecting, EntityQuery<T> query) tuple, in FixtureProxy value) =>
            {
                if (!tuple.query.TryGetComponent(value.Entity, out var comp))
                    return true;

                tuple.intersecting.Add((value.Entity, comp));
                return true;
            }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        if ((flags & LookupFlags.StaticSundries) == LookupFlags.StaticSundries)
        {
            lookup.StaticSundriesTree.QueryAabb(ref state, static (ref (HashSet<Entity<T>> intersecting, EntityQuery<T> query) tuple, in EntityUid value) =>
            {
                if (!tuple.query.TryGetComponent(value, out var comp))
                    return true;

                tuple.intersecting.Add((value, comp));
                return true;
            }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        if ((flags & LookupFlags.Sundries) != 0x0)
        {
            lookup.SundriesTree.QueryAabb(ref state, static (ref (HashSet<Entity<T>> intersecting, EntityQuery<T> query) tuple, in EntityUid value) =>
            {
                if (!tuple.query.TryGetComponent(value, out var comp))
                    return true;

                tuple.intersecting.Add((value, comp));
                return true;
            }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }
    }

    private void AddEntitiesIntersecting<T>(
        EntityUid lookupUid,
        HashSet<Entity<T>> intersecting,
        IPhysShape shape,
        Box2 worldAABB,
        LookupFlags flags,
        EntityQuery<T> query) where T : IComponent
    {
        var lookup = _broadQuery.GetComponent(lookupUid);
        var invMatrix = _transform.GetInvWorldMatrix(lookupUid);
        var localAABB = invMatrix.TransformBox(worldAABB);
        var transform = new Transform(0);
        var state = new QueryState<T>(
            intersecting,
            shape,
            transform,
            _fixtures,
            _physics,
            _transform,
            _manifoldManager,
            query,
            _fixturesQuery,
            (flags & LookupFlags.Sensors) != 0
        );

        if ((flags & LookupFlags.Dynamic) != 0x0)
        {
            lookup.DynamicTree.QueryAabb(ref state, static (ref QueryState<T> state, in FixtureProxy value) =>
            {
                if (!state.Sensors && !value.Fixture.Hard)
                    return true;

                if (!state.Query.TryGetComponent(value.Entity, out var comp))
                    return true;

                var intersectingTransform = state.Physics.GetPhysicsTransform(value.Entity);
                if (!state.Manifolds.TestOverlap(state.Shape, 0, value.Fixture.Shape, value.ChildIndex, state.Transform, intersectingTransform))
                {
                    return true;
                }

                state.Intersecting.Add((value.Entity, comp));
                return true;
            }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        if ((flags & (LookupFlags.Static)) != 0x0)
        {
            lookup.StaticTree.QueryAabb(ref state, static (ref QueryState<T> state, in FixtureProxy value) =>
            {
                if (!state.Sensors && !value.Fixture.Hard)
                    return true;

                if (!state.Query.TryGetComponent(value.Entity, out var comp))
                    return true;

                var intersectingTransform = state.Physics.GetPhysicsTransform(value.Entity);
                if (!state.Manifolds.TestOverlap(state.Shape, 0, value.Fixture.Shape, value.ChildIndex, state.Transform, intersectingTransform))
                {
                    return true;
                }

                state.Intersecting.Add((value.Entity, comp));
                return true;
            }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        if ((flags & LookupFlags.StaticSundries) == LookupFlags.StaticSundries)
        {
            lookup.StaticSundriesTree.QueryAabb(ref state, static (ref QueryState<T> state, in EntityUid value) =>
            {
                if (!state.Query.TryGetComponent(value, out var comp))
                    return true;

                var intersectingTransform = state.Physics.GetPhysicsTransform(value);

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
            }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }

        if ((flags & LookupFlags.Sundries) != 0x0)
        {
            lookup.SundriesTree.QueryAabb(ref state, static (ref QueryState<T> state,
                in EntityUid value) =>
            {
                if (!state.Query.TryGetComponent(value, out var comp))
                    return true;

                var intersectingTransform = state.Physics.GetPhysicsTransform(value);

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
            }, localAABB, (flags & LookupFlags.Approximate) != 0x0);
        }
    }

    private bool AnyComponentsIntersecting<T>(
        EntityUid lookupUid,
        Box2 worldAABB,
        LookupFlags flags,
        EntityQuery<T> query,
        EntityUid? ignored = null) where T : IComponent
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

    private void RecursiveAdd<T>(EntityUid uid, ref ValueList<T> toAdd, EntityQuery<T> query) where T : IComponent
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

    private void RecursiveAdd<T>(EntityUid uid, ref ValueList<Entity<T>> toAdd, EntityQuery<T> query) where T : IComponent
    {
        var childEnumerator = _xformQuery.GetComponent(uid).ChildEnumerator;

        while (childEnumerator.MoveNext(out var child))
        {
            if (query.TryGetComponent(child.Value, out var compies))
            {
                toAdd.Add((child.Value, compies));
            }

            RecursiveAdd(child.Value, ref toAdd, query);
        }
    }

    [Obsolete]
    private void AddContained<T>(HashSet<T> intersecting, LookupFlags flags, EntityQuery<T> query) where T : IComponent
    {
        var intersectingEntities = new HashSet<Entity<T>>();
        AddContained(intersectingEntities, flags, query);
        intersecting.UnionWith(intersectingEntities.Select(e => e.Comp));
    }

    private void AddContained<T>(HashSet<Entity<T>> intersecting, LookupFlags flags, EntityQuery<T> query) where T : IComponent
    {
        if ((flags & LookupFlags.Contained) == 0x0) return;

        var toAdd = new ValueList<Entity<T>>();

        foreach (var comp in intersecting)
        {
            if (!_containerQuery.TryGetComponent(comp, out var conManager)) continue;

            foreach (var con in conManager.GetAllContainers())
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
        DebugTools.Assert(typeof(IComponent).IsAssignableFrom(type));
        if (mapId == MapId.Nullspace) return false;

        if (!UseBoundsQuery(type, worldAABB.Height * worldAABB.Width))
        {
            foreach (var (uid, comp) in EntityManager.GetAllComponents(type, true))
            {
                var xform = _xformQuery.GetComponent(uid);

                if (xform.MapID != mapId ||
                    !worldAABB.Contains(_transform.GetWorldPosition(xform)) ||
                    ((flags & LookupFlags.Contained) == 0x0 &&
                    _container.IsEntityOrParentInContainer(uid, null, xform)))
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
                    EntityQuery<IComponent> query,
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

    public void GetEntitiesIntersecting(Type type, MapId mapId, Box2 worldAABB, HashSet<Entity<IComponent>> intersecting, LookupFlags flags = DefaultFlags)
    {
        DebugTools.Assert(typeof(IComponent).IsAssignableFrom(type));
        if (mapId == MapId.Nullspace)
            return;

        if (!UseBoundsQuery(type, worldAABB.Height * worldAABB.Width))
        {
            foreach (var (uid, comp) in EntityManager.GetAllComponents(type, true))
            {
                var xform = _xformQuery.GetComponent(uid);

                if (xform.MapID != mapId ||
                    !worldAABB.Contains(_transform.GetWorldPosition(xform)) ||
                    ((flags & LookupFlags.Contained) == 0x0 &&
                     _container.IsEntityOrParentInContainer(uid, _metaQuery.GetComponent(uid), xform)))
                {
                    continue;
                }

                intersecting.Add((uid, comp));
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
                        EntityQuery<IComponent> query,
                        HashSet<Entity<IComponent>> intersecting) tuple) =>
                {
                    tuple.system.AddEntitiesIntersecting(uid, tuple.intersecting, tuple.worldAABB, tuple.flags, tuple.query);
                    return true;
                }, (flags & LookupFlags.Approximate) != 0x0);

            // Get map entities
            var mapUid = _mapManager.GetMapEntityId(mapId);
            AddEntitiesIntersecting(mapUid, intersecting, worldAABB, flags, query);
            AddContained(intersecting, flags, query);
        }
    }

    public void GetEntitiesIntersecting<T>(MapId mapId, Box2 worldAABB, HashSet<Entity<T>> entities, LookupFlags flags = DefaultFlags) where T : IComponent
    {
        if (mapId == MapId.Nullspace) return;

        if (!UseBoundsQuery<T>(worldAABB.Height * worldAABB.Width))
        {
            var query = AllEntityQuery<T, TransformComponent>();

            while (query.MoveNext(out var uid, out var comp, out var xform))
            {
                if (xform.MapID != mapId || !worldAABB.Contains(_transform.GetWorldPosition(xform))) continue;
                entities.Add((uid, comp));
            }
        }
        else
        {
            var query = GetEntityQuery<T>();

            // Get grid entities
            var state = (this, worldAABB, flags, query, entities);

            _mapManager.FindGridsIntersecting(mapId, worldAABB, ref state,
                static (EntityUid uid, MapGridComponent grid,
                    ref (EntityLookupSystem system,
                        Box2 worldAABB,
                        LookupFlags flags,
                        EntityQuery<T> query,
                        HashSet<Entity<T>> intersecting) tuple) =>
                {
                    tuple.system.AddEntitiesIntersecting(uid, tuple.intersecting, tuple.worldAABB, tuple.flags, tuple.query);
                    return true;
                }, (flags & LookupFlags.Approximate) != 0x0);

            // Get map entities
            var mapUid = _mapManager.GetMapEntityId(mapId);
            AddEntitiesIntersecting(mapUid, entities, worldAABB, flags, query);
            AddContained(entities, flags, query);
        }
    }

    #endregion

    #region IPhysShape

    public void GetEntitiesIntersecting(Type type, MapId mapId, IPhysShape shape, HashSet<Entity<IComponent>> intersecting, LookupFlags flags = DefaultFlags)
    {
        DebugTools.Assert(typeof(IComponent).IsAssignableFrom(type));
        if (mapId == MapId.Nullspace)
            return;

        var shapeTransform = new Transform(0);
        var worldAABB = shape.ComputeAABB(shapeTransform, 0);
        var sensors = (flags & LookupFlags.Sensors) != 0;
        if (!UseBoundsQuery(type, worldAABB.Height * worldAABB.Width))
        {
            foreach (var (uid, comp) in EntityManager.GetAllComponents(type, true))
            {
                var xform = _xformQuery.GetComponent(uid);
                var (pos, rot) = _transform.GetWorldPositionRotation(xform);

                if (xform.MapID != mapId ||
                    !worldAABB.Contains(pos) ||
                    ((flags & LookupFlags.Contained) == 0x0 &&
                     _container.IsEntityOrParentInContainer(uid, _metaQuery.GetComponent(uid), xform)))
                {
                    continue;
                }

                if (_fixturesQuery.TryGetComponent(uid, out var fixtures))
                {
                    var transform = new Transform(pos, rot);

                    bool anyFixture = false;
                    foreach (var fixture in fixtures.Fixtures.Values)
                    {
                        if (!sensors && !fixture.Hard)
                            continue;

                        anyFixture = true;
                        for (var i = 0; i < fixture.Shape.ChildCount; i++)
                        {
                            if (_manifoldManager.TestOverlap(shape, 0, fixture.Shape, i, shapeTransform, transform))
                            {
                                goto found;
                            }
                        }
                    }

                    if (anyFixture)
                        continue;
                }

                if (!_fixtures.TestPoint(shape, shapeTransform, pos))
                    continue;

                found:
                intersecting.Add((uid, comp));
            }
        }
        else
        {
            var query = EntityManager.GetEntityQuery(type);

            // Get grid entities
            var state = new GridQueryState<IComponent>(intersecting, shape, worldAABB, this, flags, query);

            _mapManager.FindGridsIntersecting(mapId, worldAABB, ref state,
                static (EntityUid uid, MapGridComponent grid, ref GridQueryState<IComponent> state) =>
                {
                    state.Lookup.AddEntitiesIntersecting(uid, state.Intersecting, state.Shape, state.WorldAABB, state.Flags, state.Query);
                    return true;
                }, (flags & LookupFlags.Approximate) != 0x0);

            // Get map entities
            var mapUid = _mapManager.GetMapEntityId(mapId);
            AddEntitiesIntersecting(mapUid, intersecting, shape, worldAABB, flags, query);
            AddContained(intersecting, flags, query);
        }
    }

    public void GetEntitiesIntersecting<T>(MapId mapId, IPhysShape shape, HashSet<Entity<T>> entities, LookupFlags flags = DefaultFlags) where T : IComponent
    {
        if (mapId == MapId.Nullspace) return;

        var shapeTransform = new Transform(0);
        var worldAABB = shape.ComputeAABB(shapeTransform, 0);
        var sensors = (flags & LookupFlags.Sensors) != 0;
        if (!UseBoundsQuery<T>(worldAABB.Height * worldAABB.Width))
        {
            var query = AllEntityQuery<T, TransformComponent>();

            while (query.MoveNext(out var uid, out var comp, out var xform))
            {
                var (pos, rot) = _transform.GetWorldPositionRotation(xform);

                if (xform.MapID != mapId || !worldAABB.Contains(pos))
                    continue;

                if (_fixturesQuery.TryGetComponent(uid, out var fixtures))
                {
                    var transform = new Transform(pos, rot);
                    bool anyFixture = false;
                    foreach (var fixture in fixtures.Fixtures.Values)
                    {
                        if (!sensors && !fixture.Hard)
                            continue;

                        anyFixture = true;
                        for (var i = 0; i < fixture.Shape.ChildCount; i++)
                        {
                            if (_manifoldManager.TestOverlap(shape, 0, fixture.Shape, i, shapeTransform, transform))
                            {
                                goto found;
                            }
                        }
                    }

                    if (anyFixture)
                        continue;
                }

                if (!_fixtures.TestPoint(shape, shapeTransform, pos))
                    continue;

                found:
                entities.Add((uid, comp));
            }
        }
        else
        {
            var query = GetEntityQuery<T>();

            // Get grid entities
            var state = (this, shape, worldAABB, flags, query, entities);

            _mapManager.FindGridsIntersecting(mapId, worldAABB, ref state,
                static (EntityUid uid, MapGridComponent grid,
                    ref (EntityLookupSystem system,
                        IPhysShape shape,
                        Box2 worldAABB,
                        LookupFlags flags,
                        EntityQuery<T> query,
                        HashSet<Entity<T>> intersecting) tuple) =>
                {
                    tuple.system.AddEntitiesIntersecting(uid, tuple.intersecting, tuple.shape, tuple.worldAABB, tuple.flags, tuple.query);
                    return true;
                }, (flags & LookupFlags.Approximate) != 0x0);

            // Get map entities
            var mapUid = _mapManager.GetMapEntityId(mapId);
            AddEntitiesIntersecting(mapUid, entities, shape, worldAABB, flags, query);
            AddContained(entities, flags, query);
        }
    }

    #endregion

    #region EntityCoordinates

    public void GetEntitiesInRange<T>(EntityCoordinates coordinates, float range, HashSet<Entity<T>> entities) where T : IComponent
    {
        var mapPos = coordinates.ToMap(EntityManager, _transform);
        GetEntitiesInRange(mapPos, range, entities);
    }

    public HashSet<Entity<T>> GetEntitiesInRange<T>(EntityCoordinates coordinates, float range) where T : IComponent
    {
        var entities = new HashSet<Entity<T>>();
        GetEntitiesInRange(coordinates, range, entities);
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

    public void GetEntitiesInRange(Type type, MapCoordinates coordinates, float range, HashSet<Entity<IComponent>> entities)
    {
        DebugTools.Assert(typeof(IComponent).IsAssignableFrom(type));
        GetEntitiesInRange(type, coordinates.MapId, coordinates.Position, range, entities);
    }

    [Obsolete]
    public HashSet<T> GetComponentsInRange<T>(MapCoordinates coordinates, float range) where T : IComponent
    {
        return GetComponentsInRange<T>(coordinates.MapId, coordinates.Position, range);
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

        var circle = new PhysShapeCircle(range, worldPos);
        GetEntitiesIntersecting(type, mapId, circle, entities, flags);
    }

    [Obsolete]
    public HashSet<T> GetComponentsInRange<T>(MapId mapId, Vector2 worldPos, float range) where T : IComponent
    {
        var entities = new HashSet<Entity<T>>();
        GetEntitiesInRange(mapId, worldPos, range, entities);
        return new HashSet<T>(entities.Select(e => e.Comp));
    }

    public void GetEntitiesInRange<T>(MapId mapId, Vector2 worldPos, float range, HashSet<Entity<T>> entities, LookupFlags flags = DefaultFlags) where T : IComponent
    {
        GetEntitiesInRange(mapId, new PhysShapeCircle(range, worldPos), entities, flags);
    }

    public void GetEntitiesInRange<T>(MapId mapId, IPhysShape shape, HashSet<Entity<T>> entities, LookupFlags flags = DefaultFlags) where T : IComponent
    {
        DebugTools.Assert(shape.Radius > 0, "Range must be a positive float");

        if (mapId == MapId.Nullspace) return;

        GetEntitiesIntersecting(mapId, shape, entities, flags);
    }

    #endregion

    private readonly record struct GridQueryState<T>(
        HashSet<Entity<T>> Intersecting,
        IPhysShape Shape,
        Box2 WorldAABB,
        EntityLookupSystem Lookup,
        LookupFlags Flags,
        EntityQuery<T> Query
    ) where T : IComponent;

    private readonly record struct QueryState<T>(
        HashSet<Entity<T>> Intersecting,
        IPhysShape Shape,
        Transform Transform,
        FixtureSystem Fixtures,
        SharedPhysicsSystem Physics,
        SharedTransformSystem TransformSystem,
        IManifoldManager Manifolds,
        EntityQuery<T> Query,
        EntityQuery<FixturesComponent> FixturesQuery,
        bool Sensors
    ) where T : IComponent;
}
