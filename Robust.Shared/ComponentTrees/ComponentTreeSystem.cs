using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using System;
using System.Collections.Generic;
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

    private readonly Queue<ComponentTreeEntry<TComp>> _updateQueue = new();
    private readonly HashSet<EntityUid> _updated = new();

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

        SubscribeLocalEvent<MapChangedEvent>(MapManagerOnMapCreated);
        SubscribeLocalEvent<GridInitializeEvent>(MapManagerOnGridCreated);

        SubscribeLocalEvent<TComp, ComponentStartup>(OnCompStartup);
        SubscribeLocalEvent<TComp, ComponentRemove>(OnCompRemoved);

        if (Recursive)
        {
            SubscribeLocalEvent<TComp, TreeRecursiveMoveEvent>(HandleRecursiveMove);
            _recursiveMoveSys.AddSubscription();
        }
        else
        {
            SubscribeLocalEvent<TComp, MoveEvent>(HandleMove);
        }

        SubscribeLocalEvent<TTreeComp, EntityTerminatingEvent>(OnTerminating);
        SubscribeLocalEvent<TTreeComp, ComponentAdd>(OnTreeAdd);
        SubscribeLocalEvent<TTreeComp, ComponentRemove>(OnTreeRemove);
    }

    #region Queue Update
    private void HandleRecursiveMove(EntityUid uid, TComp component, ref TreeRecursiveMoveEvent args)
        => QueueTreeUpdate(uid, component, args.Xform);

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

    private void MapManagerOnMapCreated(MapChangedEvent e)
    {
        if (e.Destroyed || e.Map == MapId.Nullspace)
            return;

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

            if (!_updated.Add(comp.Owner))
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
    public IEnumerable<TTreeComp> GetIntersectingTrees(MapId mapId, Box2Rotated worldBounds)
        => GetIntersectingTrees(mapId, worldBounds.CalcBoundingBox());

    public IEnumerable<TTreeComp> GetIntersectingTrees(MapId mapId, Box2 worldAABB)
    {
        // Anything that queries these trees should only do so if there are no queued updates, otherwise it can lead to
        // errors. Currently there is no easy way to enforce this, but this should work as long as nothing queries the
        // trees directly:
        UpdateTreePositions();

        if (mapId == MapId.Nullspace) yield break;

        foreach (var grid in _mapManager.FindGridsIntersecting(mapId, worldAABB))
        {
            if (TryComp(grid.Owner, out TTreeComp? treeComp))
                yield return treeComp;
        }

        var mapUid = _mapManager.GetMapEntityId(mapId);

        // Don't double-iterate
        if (HasComp<MapGridComponent>(mapUid))
            yield break;

        if (TryComp(mapUid, out TTreeComp? mapTreeComp))
            yield return mapTreeComp;
    }

    public HashSet<ComponentTreeEntry<TComp>> QueryAabb(MapId mapId, Box2 worldBounds, bool approx = true)
        => QueryAabb(mapId, new Box2Rotated(worldBounds, default, default), approx);

    public HashSet<ComponentTreeEntry<TComp>> QueryAabb(MapId mapId, Box2Rotated worldBounds, bool approx = true)
    {
        var state = new HashSet<ComponentTreeEntry<TComp>>();
        foreach (var treeComp in GetIntersectingTrees(mapId, worldBounds))
        {
            var bounds = XformSystem.GetInvWorldMatrix(treeComp.Owner).TransformBox(worldBounds);

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
        foreach (var treeComp in GetIntersectingTrees(mapId, worldBounds))
        {
            var bounds = Transform(treeComp.Owner).InvWorldMatrix.TransformBox(worldBounds);
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
        var worldBox = new Box2(Vector2.ComponentMin(ray.Position, endPoint), Vector2.ComponentMax(ray.Position, endPoint));
        var xforms = GetEntityQuery<TransformComponent>();

        foreach (var comp in GetIntersectingTrees(mapId, worldBox))
        {
            var transform = xforms.GetComponent(comp.Owner);
            var (_, treeRot, matrix) = transform.GetWorldPositionRotationInvMatrix(xforms);
            var relativeAngle = new Angle(-treeRot.Theta).RotateVec(ray.Direction);
            var treeRay = new Ray(matrix.Transform(ray.Position), relativeAngle);
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

