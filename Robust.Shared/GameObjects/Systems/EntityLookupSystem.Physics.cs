using System.Collections.Generic;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Shapes;
using Robust.Shared.Physics.Systems;

namespace Robust.Shared.GameObjects;

public interface IFixtureQueryCallback<TState>
{
    /// <summary>
    /// Invoked for each matching fixture. Return false to stop the query.
    /// </summary>
    bool Invoke(ref TState state, in FixtureProxy fixture);
}

public readonly record struct FixtureQueryArgs(
    QueryFilter Filter,
    bool Approximate = false,
    bool IgnoreShapeSkin = false)
{
    public readonly QueryFilter Filter = Filter;
    public readonly bool Approximate = Approximate;
    public readonly bool IgnoreShapeSkin = IgnoreShapeSkin;
}

public sealed partial class EntityLookupSystem
{
    /// <summary>
    /// Gets fixtures intersecting the specified world AABB.
    /// </summary>
    public void GetFixturesIntersecting(
        MapId mapId,
        Box2 worldAABB,
        HashSet<FixtureProxy> fixtures,
        FixtureQueryArgs query)
    {
        ForEachFixtureIntersecting(mapId, worldAABB, ref fixtures, new AddFixtureToHashSetCallback(), query);
    }

    /// <summary>
    /// Invokes <paramref name="callback"/> for each fixture intersecting the specified world AABB.
    /// Returns false if the callback stopped the query.
    /// </summary>
    public bool ForEachFixtureIntersecting<TState, TCallback>(
        MapId mapId,
        Box2 worldAABB,
        ref TState state,
        TCallback callback,
        FixtureQueryArgs query)
        where TCallback : struct, IFixtureQueryCallback<TState>
    {
        var polygon = new SlimPolygon(worldAABB);
        return ForEachFixtureIntersecting(mapId, polygon, Physics.Transform.Empty, ref state, callback, query);
    }

    /// <summary>
    /// Gets fixtures intersecting the specified rotated world bounds.
    /// </summary>
    public void GetFixturesIntersecting(
        MapId mapId,
        Box2Rotated worldBounds,
        HashSet<FixtureProxy> fixtures,
        FixtureQueryArgs query)
    {
        ForEachFixtureIntersecting(mapId, worldBounds, ref fixtures, new AddFixtureToHashSetCallback(), query);
    }

    /// <summary>
    /// Invokes <paramref name="callback"/> for each fixture intersecting the specified rotated world bounds.
    /// Returns false if the callback stopped the query.
    /// </summary>
    public bool ForEachFixtureIntersecting<TState, TCallback>(
        MapId mapId,
        Box2Rotated worldBounds,
        ref TState state,
        TCallback callback,
        FixtureQueryArgs query)
        where TCallback : struct, IFixtureQueryCallback<TState>
    {
        var polygon = new SlimPolygon(worldBounds);
        return ForEachFixtureIntersecting(mapId, polygon, Physics.Transform.Empty, ref state, callback, query);
    }

    /// <summary>
    /// Gets fixtures intersecting a shape in world-space.
    /// </summary>
    public void GetFixturesIntersecting<TShape>(
        MapId mapId,
        TShape shape,
        Transform shapeTransform,
        HashSet<FixtureProxy> fixtures,
        FixtureQueryArgs query)
        where TShape : IPhysShape
    {
        GetFixturesIntersecting(mapId, shape, 0, shapeTransform, fixtures, query);
    }

    /// <summary>
    /// Gets fixtures intersecting a child shape in world-space.
    /// </summary>
    public void GetFixturesIntersecting<TShape>(
        MapId mapId,
        TShape shape,
        int childIndex,
        Transform shapeTransform,
        HashSet<FixtureProxy> fixtures,
        FixtureQueryArgs query)
        where TShape : IPhysShape
    {
        ForEachFixtureIntersecting(
            mapId,
            shape,
            childIndex,
            shapeTransform,
            ref fixtures,
            new AddFixtureToHashSetCallback(),
            query);
    }

    /// <summary>
    /// Invokes <paramref name="callback"/> for each fixture intersecting a shape in world-space.
    /// Returns false if the callback stopped the query.
    /// </summary>
    public bool ForEachFixtureIntersecting<TState, TShape, TCallback>(
        MapId mapId,
        TShape shape,
        Transform shapeTransform,
        ref TState state,
        TCallback callback,
        FixtureQueryArgs query)
        where TShape : IPhysShape
        where TCallback : struct, IFixtureQueryCallback<TState>
    {
        return ForEachFixtureIntersecting(mapId, shape, 0, shapeTransform, ref state, callback, query);
    }

    /// <summary>
    /// Invokes <paramref name="callback"/> for each fixture intersecting a child shape in world-space.
    /// Returns false if the callback stopped the query.
    /// </summary>
    public bool ForEachFixtureIntersecting<TState, TShape, TCallback>(
        MapId mapId,
        TShape shape,
        int childIndex,
        Transform shapeTransform,
        ref TState state,
        TCallback callback,
        FixtureQueryArgs query)
        where TShape : IPhysShape
        where TCallback : struct, IFixtureQueryCallback<TState>
    {
        if (mapId == MapId.Nullspace)
            return true;

        var worldAABB = shape.ComputeAABB(shapeTransform, childIndex);
        var queryState = new GridFixtureQueryState<TState, TShape, TCallback>(
            state,
            callback,
            shape,
            childIndex,
            shapeTransform,
            query,
            this,
            _physics);

        _map.FindGridsIntersecting(mapId, worldAABB, ref queryState,
            static (EntityUid uid, MapGridComponent grid, ref GridFixtureQueryState<TState, TShape, TCallback> state) =>
            {
                var localTransform = state.Physics.GetRelativePhysicsTransform(state.Transform, uid);
                var result = state.Lookup.ForEachLocalFixtureIntersecting(
                    uid,
                    state.Shape,
                    state.ChildIndex,
                    localTransform,
                    ref state.State,
                    state.Callback,
                    state.Query);

                if (!result)
                    state.Continue = false;

                return result;
            }, approx: true, includeMap: false);

        state = queryState.State;

        if (!queryState.Continue)
            return false;

        var mapUid = _map.GetMapOrInvalid(mapId);
        var mapTransform = _physics.GetRelativePhysicsTransform(shapeTransform, mapUid);
        return ForEachLocalFixtureIntersecting(
            mapUid,
            shape,
            childIndex,
            mapTransform,
            ref state,
            callback,
            query);
    }

    /// <summary>
    /// Invokes <paramref name="callback"/> for each fixture intersecting a shape in the local coordinates of a broadphase entity.
    /// Returns false if the callback stopped the query.
    /// </summary>
    public bool ForEachLocalFixtureIntersecting<TState, TShape, TCallback>(
        EntityUid lookupUid,
        TShape shape,
        Transform localTransform,
        ref TState state,
        TCallback callback,
        FixtureQueryArgs query,
        BroadphaseComponent? lookup = null)
        where TShape : IPhysShape
        where TCallback : struct, IFixtureQueryCallback<TState>
    {
        return ForEachLocalFixtureIntersecting(lookupUid, shape, 0, localTransform, ref state, callback, query, lookup);
    }

    /// <summary>
    /// Invokes <paramref name="callback"/> for each fixture intersecting a child shape in the local coordinates of a broadphase entity.
    /// Returns false if the callback stopped the query.
    /// </summary>
    public bool ForEachLocalFixtureIntersecting<TState, TShape, TCallback>(
        EntityUid lookupUid,
        TShape shape,
        int childIndex,
        Transform localTransform,
        ref TState state,
        TCallback callback,
        FixtureQueryArgs query,
        BroadphaseComponent? lookup = null)
        where TShape : IPhysShape
        where TCallback : struct, IFixtureQueryCallback<TState>
    {
        if (!_broadQuery.Resolve(lookupUid, ref lookup))
            return true;

        var localAABB = shape.ComputeAABB(localTransform, childIndex);
        var queryState = new FixtureQueryState<TState, TShape, TCallback>(
            state,
            callback,
            shape,
            childIndex,
            localTransform,
            query,
            _physics,
            _manifoldManager);

        if ((query.Filter.Flags & QueryFlags.Dynamic) == QueryFlags.Dynamic)
        {
            lookup.DynamicTree.QueryAabb(ref queryState, FixtureQuery, localAABB, false);
            if (!queryState.Continue)
            {
                state = queryState.State;
                return false;
            }
        }

        if ((query.Filter.Flags & QueryFlags.Static) == QueryFlags.Static)
            lookup.StaticTree.QueryAabb(ref queryState, FixtureQuery, localAABB, false);

        state = queryState.State;
        return queryState.Continue;
    }

    private static bool FixtureQuery<TState, TShape, TCallback>(
        ref FixtureQueryState<TState, TShape, TCallback> state,
        in FixtureProxy proxy)
        where TShape : IPhysShape
        where TCallback : struct, IFixtureQueryCallback<TState>
    {
        if (!FixtureMatchesFilter(proxy, state.Query.Filter))
            return true;

        if (!state.Query.Approximate)
        {
            var transform = state.Physics.GetLocalPhysicsTransform(proxy.Entity, proxy.Xform);
            if (!state.Manifolds.TestOverlap(
                    state.Shape,
                    state.ChildIndex,
                    proxy.Fixture.Shape,
                    proxy.ChildIndex,
                    state.Transform,
                    transform,
                    ignoreShapeSkin: state.Query.IgnoreShapeSkin))
            {
                return true;
            }
        }

        if (state.Callback.Invoke(ref state.State, proxy))
            return true;

        state.Continue = false;
        return false;
    }

    private static bool FixtureMatchesFilter(in FixtureProxy proxy, QueryFilter filter)
    {
        if ((filter.Flags & QueryFlags.Sensors) == 0 && !proxy.Fixture.Hard)
            return false;

        if ((proxy.Fixture.CollisionLayer & filter.MaskBits) == 0 &&
            (proxy.Fixture.CollisionMask & filter.LayerBits) == 0)
        {
            return false;
        }

        if (filter.IsIgnored?.Invoke(proxy.Entity) == true)
            return false;

        return true;
    }

    private readonly struct AddFixtureToHashSetCallback : IFixtureQueryCallback<HashSet<FixtureProxy>>
    {
        public bool Invoke(ref HashSet<FixtureProxy> state, in FixtureProxy fixture)
        {
            state.Add(fixture);
            return true;
        }
    }

    private struct GridFixtureQueryState<TState, TShape, TCallback>
        where TShape : IPhysShape
        where TCallback : struct, IFixtureQueryCallback<TState>
    {
        public TState State;
        public TCallback Callback;
        public TShape Shape;
        public int ChildIndex;
        public Transform Transform;
        public FixtureQueryArgs Query;
        public EntityLookupSystem Lookup;
        public SharedPhysicsSystem Physics;
        public bool Continue;

        public GridFixtureQueryState(
            TState state,
            TCallback callback,
            TShape shape,
            int childIndex,
            Transform transform,
            FixtureQueryArgs query,
            EntityLookupSystem lookup,
            SharedPhysicsSystem physics)
        {
            State = state;
            Callback = callback;
            Shape = shape;
            ChildIndex = childIndex;
            Transform = transform;
            Query = query;
            Lookup = lookup;
            Physics = physics;
            Continue = true;
        }
    }

    private struct FixtureQueryState<TState, TShape, TCallback>
        where TShape : IPhysShape
        where TCallback : struct, IFixtureQueryCallback<TState>
    {
        public TState State;
        public TCallback Callback;
        public TShape Shape;
        public int ChildIndex;
        public Transform Transform;
        public FixtureQueryArgs Query;
        public SharedPhysicsSystem Physics;
        public IManifoldManager Manifolds;
        public bool Continue;

        public FixtureQueryState(
            TState state,
            TCallback callback,
            TShape shape,
            int childIndex,
            Transform transform,
            FixtureQueryArgs query,
            SharedPhysicsSystem physics,
            IManifoldManager manifolds)
        {
            State = state;
            Callback = callback;
            Shape = shape;
            ChildIndex = childIndex;
            Transform = transform;
            Query = query;
            Physics = physics;
            Manifolds = manifolds;
            Continue = true;
        }
    }
}
