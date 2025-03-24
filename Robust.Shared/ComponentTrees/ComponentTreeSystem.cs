using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using System;
using System.Collections.Generic;
using Robust.Shared.Collections;
using System.Numerics;
using Robust.Shared.Map.Components;

namespace Robust.Shared.ComponentTrees;

/// <summary>
///     Keeps track of <see cref="DynamicTree{T}"/>s for various rendering-related components.
/// </summary>
[UsedImplicitly]
public abstract class ComponentTreeSystem<TTreeComp, TComp> : EntitySystem
    where TTreeComp : Component, IComponentTreeComponent<TComp>, new()
    where TComp : Component, IComponentTreeEntry<TComp>, new()
{
    [Dependency] private readonly RecursiveMoveSystem _recursiveMoveSys = default!;
    [Dependency] protected readonly SharedTransformSystem XformSystem = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;

    private readonly Queue<ComponentTreeEntry<TComp>> _updateQueue = new();
    private readonly HashSet<EntityUid> _updated = new();
    protected EntityQuery<TComp> Query;

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

    public override void Initialize()
    {
        base.Initialize();

        UpdatesOutsidePrediction = DoTickUpdate;
        UpdatesAfter.Add(typeof(SharedTransformSystem));
        UpdatesAfter.Add(typeof(SharedPhysicsSystem));

        SubscribeLocalEvent<MapCreatedEvent>(MapManagerOnMapCreated);
        SubscribeLocalEvent<GridInitializeEvent>(MapManagerOnGridCreated);

        SubscribeLocalEvent<TComp, ComponentStartup>(OnCompStartup);
        SubscribeLocalEvent<TComp, ComponentRemove>(OnCompRemoved);

        if (Recursive)
        {
            _recursiveMoveSys.OnTreeRecursiveMove += HandleRecursiveMove;
            _recursiveMoveSys.AddSubscription();
        }
        else
        {
            // TODO EXCEPTION TOLERANCE
            // Ensure lookup trees update before content code handles move events.
            SubscribeLocalEvent<TComp, MoveEvent>(HandleMove);
        }

        SubscribeLocalEvent<TTreeComp, EntityTerminatingEvent>(OnTerminating);
        SubscribeLocalEvent<TTreeComp, ComponentAdd>(OnTreeAdd);
        SubscribeLocalEvent<TTreeComp, ComponentRemove>(OnTreeRemove);

        Query = GetEntityQuery<TComp>();
    }

    public override void Shutdown()
    {
        if (Recursive)
        {
            _recursiveMoveSys.OnTreeRecursiveMove -= HandleRecursiveMove;
        }
    }

    #region Queue Update

    private void HandleRecursiveMove(EntityUid uid, TransformComponent xform)
    {
        if (Query.TryGetComponent(uid, out var component))
            QueueTreeUpdate(uid, component, xform);
    }

    private void HandleMove(EntityUid uid, TComp component, ref MoveEvent args)
        => QueueTreeUpdate(uid, component, args.Component);

    public void QueueTreeUpdate(EntityUid uid, TComp component, TransformComponent? xform = null)
    {
        if (component.TreeUpdateQueued || !Resolve(uid, ref xform))
            return;

        component.TreeUpdateQueued = true;
        _updateQueue.Enqueue((component, xform));
    }
    #endregion

    #region Component Management
    protected virtual void OnCompStartup(EntityUid uid, TComp component, ComponentStartup args)
        => QueueTreeUpdate(uid, component);

    protected virtual void OnCompRemoved(EntityUid uid, TComp component, ComponentRemove args)
        => RemoveFromTree(component);

    protected virtual void OnTreeAdd(EntityUid uid, TTreeComp component, ComponentAdd args)
    {
        component.Tree = new(ExtractAabb, capacity: InitialCapacity);
    }

    protected virtual void OnTreeRemove(EntityUid uid, TTreeComp component, ComponentRemove args)
    {
        if (Terminating(uid))
            return;

        foreach (var entry in component.Tree)
        {
            entry.Component.TreeUid = null;
        }

        component.Tree.Clear();
    }

    protected virtual void OnTerminating(EntityUid uid, TTreeComp component, ref EntityTerminatingEvent args)
    {
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
        if (DoTickUpdate)
            UpdateTreePositions();
    }

    public override void FrameUpdate(float frameTime)
    {
        if (DoFrameUpdate)
            UpdateTreePositions();
    }

    /// <summary>
    ///     Processes any pending position updates. Note that this should generally always get run before directly
    ///     querying a tree.
    /// </summary>
    public void UpdateTreePositions()
    {
        if (_updateQueue.Count == 0)
            return;

        var xforms = GetEntityQuery<TransformComponent>();
        var trees = GetEntityQuery<TTreeComp>();

        while (_updateQueue.TryDequeue(out var entry))
        {
            var (comp, xform) = entry;

            comp.TreeUpdateQueued = false;
            if (!comp.Running)
                continue;

            if (!_updated.Add(entry.Uid))
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
                    newTree!.Value,
                    xforms);

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
                newTree!.Value,
                xforms);

            newTreeComp.Tree.Add(entry, ExtractAabb(entry, pos, rot));
        }

        _updated.Clear();
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
            entry.Component.TreeUid.Value,
            GetEntityQuery<TransformComponent>());

        return ExtractAabb(in entry, pos, rot);
    }

    protected abstract Box2 ExtractAabb(in ComponentTreeEntry<TComp> entry, Vector2 pos, Angle rot);
    #endregion

    #region Queries
    public IEnumerable<(EntityUid, TTreeComp)> GetIntersectingTrees(MapId mapId, Box2Rotated worldBounds)
        => GetIntersectingTrees(mapId, worldBounds.CalcBoundingBox());

    public IEnumerable<(EntityUid Uid, TTreeComp Comp)> GetIntersectingTrees(MapId mapId, Box2 worldAABB)
    {
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

    public HashSet<ComponentTreeEntry<TComp>> QueryAabb(MapId mapId, Box2 worldBounds, bool approx = true)
        => QueryAabb(mapId, new Box2Rotated(worldBounds, default, default), approx);

    public HashSet<ComponentTreeEntry<TComp>> QueryAabb(MapId mapId, Box2Rotated worldBounds, bool approx = true)
    {
        var state = new HashSet<ComponentTreeEntry<TComp>>();
        foreach (var (tree, treeComp) in GetIntersectingTrees(mapId, worldBounds))
        {
            var bounds = XformSystem.GetInvWorldMatrix(tree).TransformBox(worldBounds);

            treeComp.Tree.QueryAabb(ref state, static (ref HashSet<ComponentTreeEntry<TComp>> state, in ComponentTreeEntry<TComp> value) =>
            {
                state.Add(value);
                return true;
            },
            bounds, approx);
        }
        return state;
    }

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
        foreach (var (tree, treeComp) in GetIntersectingTrees(mapId, worldBounds))
        {
            var bounds = XformSystem.GetInvWorldMatrix(tree).TransformBox(worldBounds);
            treeComp.Tree.QueryAabb(ref state, callback, bounds, approx);
        }
    }

    public List<RayCastResults> IntersectRayWithPredicate<TState>(MapId mapId, in Ray ray, float maxLength,
        TState state, Func<EntityUid, TState, bool> predicate, bool returnOnFirstHit = true)
    {
        if (mapId == MapId.Nullspace)
            return new ();

        var queryState = new QueryState<TState>(maxLength, returnOnFirstHit, state, predicate);

        var endPoint = ray.Position + ray.Direction * maxLength;
        var worldBox = new Box2(Vector2.Min(ray.Position, endPoint), Vector2.Max(ray.Position, endPoint));

        foreach (var (treeUid, comp) in GetIntersectingTrees(mapId, worldBox))
        {
            var (_, treeRot, matrix) = XformSystem.GetWorldPositionRotationInvMatrix(treeUid);
            var relativeAngle = new Angle(-treeRot.Theta).RotateVec(ray.Direction);
            var treeRay = new Ray(Vector2.Transform(ray.Position, matrix), relativeAngle);
            comp.Tree.QueryRay(ref queryState, QueryCallback, treeRay);
            if (returnOnFirstHit && queryState.List.Count > 0)
                break;
        }

        return queryState.List;

        static bool QueryCallback(
            ref QueryState<TState> state,
            in ComponentTreeEntry<TComp> value,
            in Vector2 point,
            float distFromOrigin)
        {
            if (distFromOrigin > state.MaxLength || state.Predicate.Invoke(value.Uid, state.State))
                return true;

            state.List.Add(new RayCastResults(distFromOrigin, point, value.Uid));
            return !state.ReturnOnFirstHit;
        }
    }

    private readonly struct QueryState<TState>
    {
        public readonly float MaxLength;
        public readonly bool ReturnOnFirstHit;
        public readonly List<RayCastResults> List = new();
        public readonly TState State;
        public readonly Func<EntityUid, TState, bool> Predicate;

        public QueryState(float maxLength, bool returnOnFirstHit, TState state, Func<EntityUid, TState, bool>  predictate)
        {
            MaxLength = maxLength;
            ReturnOnFirstHit = returnOnFirstHit;
            State = state;
            Predicate = predictate;
        }
    }
    #endregion
}

