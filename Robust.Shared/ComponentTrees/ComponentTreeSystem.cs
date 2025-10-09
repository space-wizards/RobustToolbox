using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Collections;
using System.Numerics;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;

namespace Robust.Shared.ComponentTrees;

/// <summary>
///     Keeps track of <see cref="DynamicTree{T}"/>s for various rendering-related components.
/// </summary>
[UsedImplicitly]
public abstract class ComponentTreeSystem<TTreeComp, TComp> : EntitySystem
    where TTreeComp : Component, IComponentTreeComponent<TComp>, new()
    where TComp : Component, IComponentTreeEntry<TComp>
{
    [Dependency] private readonly RecursiveMoveSystem _recursiveMoveSys = default!;
    [Dependency] protected readonly SharedTransformSystem XformSystem = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;

    private readonly Queue<ComponentTreeEntry<TComp>> _updateQueue = new();
    protected EntityQuery<TComp> Query;

    /// <summary>
    /// Whether this lookup tree should even be enabled.
    /// </summary>
    /// <remarks>
    /// This can be used to disable some trees if they are not required, which helps improve performance a bit.
    /// </remarks>
    protected virtual bool Enabled => true;
    private bool _initialized;

    /// <summary>
    ///     If true, this system will update the tree positions every frame update. See also <see cref="DoTickUpdate"/>. Some systems may need to do both.
    /// </summary>
    protected abstract bool DoFrameUpdate { get; }

    /// <summary>
    ///     If true, this system will update the tree positions every tick update. See also <see cref="DoFrameUpdate"/>. Some systems may need to do both.
    /// </summary>
    protected abstract bool DoTickUpdate { get; }

    /// <summary>
    ///     Initial tree capacity. Note that client-side trees will remove entities as they leave PVS range.
    /// </summary>
    protected virtual int InitialCapacity { get; } = 256;

    /// <summary>
    ///     If true, this tree requires all children to be recursively updated whenever ANY entity moves. If false, this
    ///     will only update when an entity with the given component moves.
    /// </summary>
    protected abstract bool Recursive { get; }

    private bool _recursive;

    public override void Initialize()
    {
        base.Initialize();

        if (!Enabled)
            return;

        _initialized = true;
        UpdatesOutsidePrediction = DoTickUpdate;
        UpdatesAfter.Add(typeof(SharedTransformSystem));
        UpdatesAfter.Add(typeof(SharedPhysicsSystem));

        SubscribeLocalEvent<MapCreatedEvent>(MapManagerOnMapCreated);
        SubscribeLocalEvent<GridInitializeEvent>(MapManagerOnGridCreated);

        SubscribeLocalEvent<TComp, ComponentStartup>(OnCompStartup);
        SubscribeLocalEvent<TComp, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<TComp, MetaFlagRemoveAttemptEvent>(OnFlagRemoveAttempt);
        SubscribeLocalEvent<TComp, ComponentRemove>(OnCompRemoved);

        _recursive = Recursive;
        if (_recursive)
        {
            _recursiveMoveSys.AddSubscription();
            _recursiveMoveSys.OnRecursiveMove += HandleRecursiveMove;
            SubscribeLocalEvent<TComp, HasRecursiveTreeCompEvent>(OnHasComp);
        }
        else
        {
            _recursiveMoveSys.OnCompMoved += HandleMove;
        }

        SubscribeLocalEvent<TTreeComp, EntityTerminatingEvent>(OnTerminating);
        SubscribeLocalEvent<TTreeComp, ComponentAdd>(OnTreeAdd);
        SubscribeLocalEvent<TTreeComp, ComponentRemove>(OnTreeRemove);

        Query = GetEntityQuery<TComp>();
    }

    public override void Shutdown()
    {
        if (!_initialized)
            return;

        _initialized = false;

        if (_recursive)
            _recursiveMoveSys.OnRecursiveMove -= HandleRecursiveMove;
        else
            _recursiveMoveSys.OnCompMoved -= HandleMove;
    }

    private bool CheckEnabled()
    {
        if (_initialized)
            return true;

        Log.Error($"Attempted to use disabled lookup tree");
        return false;
    }

    #region Queue Update

    private void HandleMove(EntityUid uid, TransformComponent xform)
    {
        if (Query.TryGetComponentInternal(uid, out var component))
            QueueTreeUpdate(uid, component, xform);
    }

    private void HandleRecursiveMove(ReadOnlySpan<EntityUid> entities)
    {
        foreach (var uid in entities)
        {
            if (Query.TryGetComponentInternal(uid, out var component))
                QueueTreeUpdate(uid, component, Transform(uid));
        }
    }

    public void QueueTreeUpdate(EntityUid uid, TComp component, TransformComponent? xform = null)
    {
        if (!_initialized)
            return;

        if (component.TreeUpdateQueued || !Resolve(uid, ref xform))
            return;

        component.TreeUpdateQueued = true;
        _updateQueue.Enqueue((component, xform));
    }

    public void QueueTreeUpdate(Entity<TComp> entity, TransformComponent? xform = null)
    {
        QueueTreeUpdate(entity.Owner, entity.Comp, xform);
    }
    #endregion

    #region Component Management
    protected virtual void OnCompInit(Entity<TComp> ent, ref ComponentInit args)
    {
        QueueTreeUpdate(ent.Owner, ent.Comp);

        if (Recursive)
            _recursiveMoveSys.OnCompAdded(ent.Owner);
        else
            _meta.AddFlag(ent.Owner, MetaDataFlags.CompTree);
    }

    protected virtual void OnCompStartup(EntityUid uid, TComp component, ComponentStartup args)
    {
    }

    protected virtual void OnCompRemoved(EntityUid uid, TComp component, ComponentRemove args)
    {
        if (Recursive)
            _recursiveMoveSys.OnCompRemoved(uid);
        else
            _meta.RemoveFlag(uid, MetaDataFlags.CompTree);
        RemoveFromTree(component);
    }

    private void OnHasComp(Entity<TComp> ent, ref HasRecursiveTreeCompEvent args)
    {
        if (ent.Comp is {LifeStage: <= ComponentLifeStage.Running})
            args.Result = true;
    }

    protected virtual void OnFlagRemoveAttempt(Entity<TComp> ent, ref MetaFlagRemoveAttemptEvent args)
    {
        if (ent.Comp is {LifeStage: <= ComponentLifeStage.Running})
            args.ToRemove &= ~MetaDataFlags.CompTree;
    }

    protected virtual void OnTreeAdd(EntityUid uid, TTreeComp component, ComponentAdd args)
    {
        component.Tree = new(ExtractAabb, capacity: InitialCapacity);
    }

    protected virtual void OnTreeRemove(EntityUid uid, TTreeComp component, ComponentRemove args)
    {
        foreach (var entry in component.Tree)
        {
            entry.Component.TreeUid = null;
            entry.Component.Tree = null;
        }

        component.Tree.Clear();
    }

    protected virtual void OnTerminating(EntityUid uid, TTreeComp component, ref EntityTerminatingEvent args)
    {
        // IIRC, this is to prevent a tree-update spam as each of the entity's children get detached to nullspace.
        RemComp(uid, component);
    }

    private void MapManagerOnMapCreated(MapCreatedEvent e)
    {
        EnsureComp<TTreeComp>(e.Uid);
    }

    private void MapManagerOnGridCreated(GridInitializeEvent ev)
    {
        EnsureComp<TTreeComp>(ev.EntityUid);
    }
    #endregion

    #region Update Trees
    public override void Update(float frameTime)
    {
        if (DoTickUpdate && _initialized)
            UpdateTreePositions();
    }

    public override void FrameUpdate(float frameTime)
    {
        if (DoFrameUpdate &&  _initialized)
            UpdateTreePositions();
    }

    /// <summary>
    ///     Processes any pending position updates. Note that this should generally always get run before directly
    ///     querying a tree.
    /// </summary>
    public void UpdateTreePositions()
    {
        if (!CheckEnabled())
            return;

        if (_updateQueue.Count == 0)
            return;

        var trees = GetEntityQuery<TTreeComp>();

        while (_updateQueue.TryDequeue(out var entry))
        {
            var (comp, xform) = entry;

            // Was this entity queued multiple times?
            DebugTools.Assert(comp.TreeUpdateQueued, "Entity was queued multiple times?");

            comp.TreeUpdateQueued = false;
            if (!comp.Running)
                continue;

            if (!comp.AddToTree || comp.Deleted || xform.MapUid == null)
            {
                RemoveFromTree(comp);
                continue;
            }

            var newTree = xform.GridUid ?? xform.MapUid;
            if (!trees.TryGetComponent(newTree, out var newTreeComp) && comp.TreeUid == null)
                continue;

            Vector2 pos;
            Angle rot;
            if (comp.TreeUid == newTree)
            {
                (pos, rot) = XformSystem.GetRelativePositionRotation(
                    entry.Transform,
                    newTree!.Value);

                newTreeComp!.Tree.Update(entry, ExtractAabb(entry, pos, rot));
                continue;
            }

            RemoveFromTree(comp);

            if (newTreeComp == null)
                return;

            comp.TreeUid = newTree;
            comp.Tree = newTreeComp.Tree;

            (pos, rot) = XformSystem.GetRelativePositionRotation(
                entry.Transform,
                newTree!.Value);

            newTreeComp.Tree.Add(entry, ExtractAabb(entry, pos, rot));
        }
    }

    private void RemoveFromTree(TComp component)
    {
        component.Tree?.Remove(new() { Component = component });
        component.Tree = null;
        component.TreeUid = null;
    }
    #endregion

    #region AABBs
    protected virtual Box2 ExtractAabb(in ComponentTreeEntry<TComp> entry)
    {
        if (entry.Component.TreeUid == null)
            return default;

        var (pos, rot) = XformSystem.GetRelativePositionRotation(
            entry.Transform,
            entry.Component.TreeUid.Value);

        return ExtractAabb(in entry, pos, rot);
    }

    protected abstract Box2 ExtractAabb(in ComponentTreeEntry<TComp> entry, Vector2 pos, Angle rot);
    #endregion

    #region Queries
    public IEnumerable<(EntityUid, TTreeComp)> GetIntersectingTrees(MapId mapId, Box2Rotated worldBounds)
        => GetIntersectingTrees(mapId, worldBounds.CalcBoundingBox());

    public IEnumerable<(EntityUid Uid, TTreeComp Comp)> GetIntersectingTrees(MapId mapId, Box2 worldAABB)
    {
        if (!CheckEnabled())
            return [];
        // Anything that queries these trees should only do so if there are no queued updates, otherwise it can lead to
        // errors. Currently there is no easy way to enforce this, but this should work as long as nothing queries the
        // trees directly:
        UpdateTreePositions();
        var trees = new ValueList<(EntityUid Uid, TTreeComp Comp)>();

        if (mapId == MapId.Nullspace)
            return trees;

        var state = (EntityManager, trees);

        _mapManager.FindGridsIntersecting(mapId, worldAABB, ref state,
            (EntityUid uid, MapGridComponent grid,
                ref (EntityManager EntityManager, ValueList<(EntityUid, TTreeComp)> trees) tuple) =>
            {
                if (tuple.EntityManager.TryGetComponent<TTreeComp>(uid, out var treeComp))
                {
                    tuple.trees.Add((uid, treeComp));
                }

                return true;
            }, includeMap: false);

        if (_mapSystem.TryGetMap(mapId, out var mapUid) && TryComp(mapUid, out TTreeComp? mapTreeComp))
        {
            state.trees.Add((mapUid.Value, mapTreeComp));
        }

        return state.trees;
    }

    #region HashSet

    public HashSet<ComponentTreeEntry<TComp>> QueryAabb(MapId mapId, Box2 worldBounds, bool approx = true)
        => QueryAabb(mapId, new Box2Rotated(worldBounds, default, default), approx);

    public void QueryAabb(HashSet<Entity<TComp, TransformComponent>> results, MapId mapId, Box2 worldBounds, bool approx = true)
        => QueryAabb(results, mapId, new Box2Rotated(worldBounds, default, default), approx);

    public HashSet<ComponentTreeEntry<TComp>> QueryAabb(MapId mapId, Box2Rotated worldBounds, bool approx = true)
    {
        var state = new HashSet<ComponentTreeEntry<TComp>>();
        QueryAabb(state, mapId, worldBounds, approx);

        return state;
    }

    [Obsolete("Use Entity<T> variant")]
    internal void QueryAabb(
        HashSet<ComponentTreeEntry<TComp>> results,
        MapId mapId,
        Box2Rotated worldBounds,
        bool approx = true)
    {
        if (!CheckEnabled())
            return;

        foreach (var (tree, treeComp) in GetIntersectingTrees(mapId, worldBounds))
        {
            var bounds = XformSystem.GetInvWorldMatrix(tree).TransformBox(worldBounds);

            treeComp.Tree.QueryAabb(ref results,
                static (ref HashSet<ComponentTreeEntry<TComp>> state, in ComponentTreeEntry<TComp> value) =>
                {
                    state.Add(value);
                    return true;
                },
                bounds,
                approx);
        }
    }

    public void QueryAabb(
        HashSet<Entity<TComp, TransformComponent>> results,
        MapId mapId,
        Box2Rotated worldBounds,
        bool approx = true)
    {
        if (!CheckEnabled())
            return;

        foreach (var (tree, treeComp) in GetIntersectingTrees(mapId, worldBounds))
        {
            var bounds = XformSystem.GetInvWorldMatrix(tree).TransformBox(worldBounds);

            treeComp.Tree.QueryAabb(ref results,
                static (ref HashSet<Entity<TComp, TransformComponent>> state, in ComponentTreeEntry<TComp> value) =>
                {
                    state.Add(value);
                    return true;
                },
                bounds,
                approx);
        }
    }

    #endregion


    #region List

    public void QueryAabb(List<Entity<TComp, TransformComponent>> results, MapId mapId, Box2 worldBounds, bool approx = true)
        => QueryAabb(results, mapId, new Box2Rotated(worldBounds, default, default), approx);

    public void QueryAabb(
        List<Entity<TComp, TransformComponent>> results,
        MapId mapId,
        Box2Rotated worldBounds,
        bool approx = true)
    {
        if (!CheckEnabled())
            return;

        foreach (var (tree, treeComp) in GetIntersectingTrees(mapId, worldBounds))
        {
            var bounds = XformSystem.GetInvWorldMatrix(tree).TransformBox(worldBounds);

            treeComp.Tree.QueryAabb(ref results,
                static (ref List<Entity<TComp, TransformComponent>> state, in ComponentTreeEntry<TComp> value) =>
                {
                    state.Add(value);
                    return true;
                },
                bounds,
                approx);
        }
    }

    #endregion

    public void QueryAabb<TState>(
        ref TState state,
        DynamicTree<ComponentTreeEntry<TComp>>.QueryCallbackDelegate<TState> callback,
        MapId mapId,
        Box2 worldBounds,
        bool approx = true)
    {
        QueryAabb(ref state, callback, mapId, new Box2Rotated(worldBounds, default, default), approx);
    }

    public void QueryAabb<TState>(
        ref TState state,
        DynamicTree<ComponentTreeEntry<TComp>>.QueryCallbackDelegate<TState> callback,
        MapId mapId,
        Box2Rotated worldBounds,
        bool approx = true)
    {
        if (!CheckEnabled())
            return;

        foreach (var (tree, treeComp) in GetIntersectingTrees(mapId, worldBounds))
        {
            var bounds = XformSystem.GetInvWorldMatrix(tree).TransformBox(worldBounds);
            treeComp.Tree.QueryAabb(ref state, callback, bounds, approx);
        }
    }


    #endregion

    #region Rays

    [Obsolete("use IntersectRay")]
    public List<RayCastResults> IntersectRayWithPredicate<TState>(MapId mapId, in Ray ray, float maxLength,
        TState state, Func<EntityUid, TState, bool> predicate, bool returnOnFirstHit = true)
    {
        var list = new List<RayCastResults>();

        if (!returnOnFirstHit)
        {
            IntersectRay(list, mapId, ray, maxLength, state, (e, s) => predicate(e.Owner, s));
            return list;
        }

        var result = IntersectRay(mapId, ray, maxLength, state, (e, s) => predicate(e.Owner, s));
        if (result != null)
            list.Add(result.Value);
        return list;
    }

    /// <summary>
    /// Perform a ray intersection and return on the first hit.
    /// </summary>
    public RayCastResults? IntersectRay(MapId mapId, in Ray ray, float length)
    {
        var state = new QueryState(length);
        IntersectRayInternal(mapId, in ray, length, ref state, QueryCallback);
        return state.Result;
    }

    /// <summary>
    /// Perform a ray intersection and populate a provided list of results.
    /// </summary>
    public void IntersectRay(List<RayCastResults> results, MapId mapId, in Ray ray, float maxLength)
    {
        results.Clear();
        var state = new QueryState(maxLength, results);
        IntersectRayInternal(mapId, in ray, maxLength, ref state, QueryCallback);
    }

    /// <summary>
    /// Perform a ray intersection with a predicate and return on the first hit.
    /// </summary>
    public RayCastResults? IntersectRay<TState>(
        MapId mapId,
        in Ray ray,
        float length,
        TState predicateState,
        Func<Entity<TComp, TransformComponent>, TState, bool> ignore)
    {
        var state = new QueryState<TState>(new(length), predicateState, ignore);
        IntersectRayInternal(mapId, in ray, length, ref state, PredicateQueryCallback);
        return state.Inner.Result;
    }

    /// <summary>
    /// Perform a ray intersection with a predicate and populate a provided list of results.
    /// </summary>
    public void IntersectRay<TState>(
        List<RayCastResults> results,
        MapId mapId,
        in Ray ray,
        float length,
        TState predicateState,
        Func<Entity<TComp, TransformComponent>, TState, bool> ignore)
    {
        var state = new QueryState<TState>(new(length, results), predicateState, ignore);
        IntersectRayInternal(mapId, in ray, length, ref state, PredicateQueryCallback);
    }

    private void IntersectRayInternal<TState>(
        MapId mapId,
        in Ray ray,
        float maxLength,
        ref TState state,
        DynamicTree<ComponentTreeEntry<TComp>>.RayQueryCallbackDelegate<TState> callback)
        where TState : IDone
    {
        if (mapId == MapId.Nullspace)
            return;

        if (!CheckEnabled())
            return;
        var endPoint = ray.Position + ray.Direction * maxLength;
        var worldBox = new Box2(Vector2.Min(ray.Position, endPoint), Vector2.Max(ray.Position, endPoint));

        foreach (var (treeUid, comp) in GetIntersectingTrees(mapId, worldBox))
        {
            var (_, treeRot, matrix) = XformSystem.GetWorldPositionRotationInvMatrix(treeUid);
            var relativeAngle = new Angle(-treeRot.Theta).RotateVec(ray.Direction);
            var treeRay = new Ray(Vector2.Transform(ray.Position, matrix), relativeAngle);
            comp.Tree.QueryRay(ref state, callback, treeRay);
            if (state.Done)
                return;
        }
    }

    static bool QueryCallback(
        ref QueryState state,
        in ComponentTreeEntry<TComp> value,
        in Vector2 point,
        float dist)
    {
        if (dist > state.MaxLength)
            return true;

        if (state.ReturnOnFirstHit)
        {
            state.Result = new RayCastResults(dist, point, value.Uid);
            return false;
        }

        state.List.Add(new RayCastResults(dist, point, value.Uid));
        return true;
    }

    private static bool PredicateQueryCallback<TState>(
        ref QueryState<TState> state,
        in ComponentTreeEntry<TComp> value,
        in Vector2 point,
        float dist)
    {
        if (dist > state.Inner.MaxLength)
            return true;

        if (state.Ignore.Invoke(value, state.PredicateState))
            return true;

        if (state.Inner.ReturnOnFirstHit)
        {
            state.Inner.Result = new RayCastResults(dist, point, value.Uid);
            return false;
        }

        state.Inner.List.Add(new RayCastResults(dist, point, value.Uid));
        return true;
    }

    private struct QueryState<TPredicateState>(
        QueryState inner,
        TPredicateState predicateState,
        Func<Entity<TComp, TransformComponent>, TPredicateState, bool> ignore) : IDone
    {
        public readonly TPredicateState PredicateState = predicateState;
        public readonly Func<Entity<TComp, TransformComponent>, TPredicateState, bool> Ignore = ignore;
        public QueryState Inner = inner;
        public bool Done => Inner.Done;
    }

    private struct QueryState(float maxLength, List<RayCastResults>? list = null) : IDone
    {
        public readonly float MaxLength = maxLength;

        [MemberNotNullWhen(false, nameof(List))]
        public readonly bool ReturnOnFirstHit => List == null;
        public readonly List<RayCastResults>? List = list;
        public RayCastResults? Result;
        public bool Done => Result != null;
    }

    private interface IDone
    {
        bool Done { get; }
    }

    #endregion
}
