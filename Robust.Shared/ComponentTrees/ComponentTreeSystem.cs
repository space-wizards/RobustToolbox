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
using System.Linq;

namespace Robust.Shared.ComponentTrees;

#region FLAT

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

    protected readonly Queue<ComponentTreeEntry<TComp>> _updateQueue = new();
    protected readonly HashSet<EntityUid> _updated = new();
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

        SubscribeLocalEvent<MapChangedEvent>(MapManagerOnMapCreated);
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

    public virtual void QueueTreeUpdate(EntityUid uid, TComp component, TransformComponent? xform = null)
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

            // Our new tree should belong to either our current grid or our current map
            var newTreeId = xform.GridUid ?? xform.MapUid;

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

            // Attempt to grab the tree from the current entry's grid/map
            if (!trees.TryGetComponent(newTreeId, out var newTreeComp) && comp.TreeUid == null)
                continue;

            AddOrUpdateTreeEntry(newTreeId!.Value, newTreeComp, entry, xforms);
        }

        _updated.Clear();
    }

    /// <summary>
    /// Handles updating a given queue entry's tree position.\n
    /// Removes the component from the tree if it's in a different tree than the one it's being added to.\n
    /// And then adds or updates the component in the given tree\n
    /// </summary>
    /// <remarks> //
    /// If running in a loop, pass <paramref name="xforms"/> to avoid excessively querying the xForm query
    /// </remarks>
    /// <param name="newTreeId"></param>
    /// <param name="newTreeComp"></param>
    /// <param name="entry"></param>
    /// <param name="xforms"></param>
    protected virtual void AddOrUpdateTreeEntry(EntityUid newTreeId, TTreeComp? newTreeComp, in ComponentTreeEntry<TComp> entry, EntityQuery<TransformComponent>? xforms, DynamicTree<ComponentTreeEntry<TComp>>? tree = null)
    {

        xforms ??= GetEntityQuery<TransformComponent>();

        // If the component is currently in another tree, remove it
        if (entry.Component.TreeUid != newTreeId) RemoveFromTree(entry.Component);

        // If we have nothing to add to, then leave
        if (newTreeComp == null) return;

        // If no tree was passed, get use the newTreeComp's
        tree ??= newTreeComp.Tree;

        Vector2 pos;
        Angle rot;

        (pos, rot) = XformSystem.GetRelativePositionRotation(entry.Transform, newTreeId, xforms.Value);

        entry.Component.TreeUid = newTreeId;
        entry.Component.Tree = tree;

        tree.AddOrUpdate(entry, ExtractAabb(entry, pos, rot));


    }
    protected virtual void RemoveFromTree(TComp component)
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

        var mapUid = _mapManager.GetMapEntityId(mapId);

        if (TryComp(mapUid, out TTreeComp? mapTreeComp))
        {
            state.trees.Add((mapUid, mapTreeComp));
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

#endregion


#region LAYERED

/// <summary>
///     Keeps track of layered <see cref="DynamicTree{T}"/>s for various rendering-related components.
/// </summary>
[UsedImplicitly]
public abstract class LayeredComponentTreeSystem<TLayeredTreeComp, TLayeredComp> : ComponentTreeSystem<TLayeredTreeComp, TLayeredComp>
    where TLayeredTreeComp : Component, ILayeredComponentTreeComponent<TLayeredComp>, new()
    where TLayeredComp : Component, ILayeredComponentTreeEntry<TLayeredComp>, new()
{

    /// <summary>
    ///     Initial number of trees to generate as separate "layers"
    ///     i.e, number of drawDepth layers for sprites, etc.
    /// </summary>
    /// <value></value>
    protected virtual int InitialLayers { get; } = 1;

    protected override void OnTreeAdd(EntityUid uid, TLayeredTreeComp component, ComponentAdd args)
    {
        // Standard flat tree handling
        base.OnTreeAdd(uid, component, args);
        // Initialize layers as well
        component.Trees = new();
        for (int i = 0; i < InitialLayers; i++)
        {
            GetOrCreateLayer(component, i);
        }
    }

    protected virtual DynamicTree<ComponentTreeEntry<TLayeredComp>> GetOrCreateLayer(TLayeredTreeComp treeComp, int layer)
    {
        if (!treeComp.Trees.TryGetValue(layer, out var tree))
        {
            tree = new(ExtractAabb, capacity: InitialCapacity);
            treeComp.Trees.Add(layer, tree);
        }
        return tree;
    }
    protected override void OnTreeRemove(EntityUid uid, TLayeredTreeComp component, ComponentRemove args)
    {
        if (Terminating(uid))
            return;
        base.OnTreeRemove(uid, component, args);
        foreach (var tree in component.Trees)
        {
            foreach (var entry in tree.Value)
            {
                entry.Component.TreeUid = null;
            }
            tree.Value.Clear();
        }
        component.Trees.Clear();
    }


    protected override void AddOrUpdateTreeEntry(EntityUid newTreeId, TLayeredTreeComp? newTreeComp, in ComponentTreeEntry<TLayeredComp> entry, EntityQuery<TransformComponent>? xforms, DynamicTree<ComponentTreeEntry<TLayeredComp>>? tree = null)
    {
        base.AddOrUpdateTreeEntry(newTreeId, newTreeComp, entry, xforms, tree);

        // If no tree was passed, repeat this update for each layer in our treeComp
        if (tree == null && newTreeComp != null)
        {

            foreach (var layerIndex in entry.Component.LayersUsed)
            {
                // GetOrCreateLayer will ensure we have a layer to add to
                var treeLayer = GetOrCreateLayer(newTreeComp, layerIndex);
                AddOrUpdateTreeEntry(newTreeId, newTreeComp, entry, xforms, treeLayer);
            }

            if (entry.Component.TreeUid == newTreeId) entry.Component.Trees = newTreeComp.Trees;
        }
    }

    protected override void RemoveFromTree(TLayeredComp component)
    {
        if (component.Trees != null)
        {
            foreach (var tree in component.Trees.Values)
            {
                tree.Remove(new() { Component = component });
            }
        }
        component.Trees = null;
        base.RemoveFromTree(component);
    }

    #region Queries
    public IEnumerable<(EntityUid, TLayeredTreeComp, DynamicTree<ComponentTreeEntry<TLayeredComp>>, int layerIndex)> GetIntersectingTreeLayers(MapId mapId, Box2Rotated worldBounds, int[]? layers = null)
        => GetIntersectingTreeLayers(mapId, worldBounds.CalcBoundingBox(), layers);
    public IEnumerable<(EntityUid, TLayeredTreeComp, DynamicTree<ComponentTreeEntry<TLayeredComp>>, int layerIndex)> GetIntersectingTreeLayers(MapId mapId, Box2 worldBox, int[]? layers = null)
    {
        foreach (var (tree, treeComp) in GetIntersectingTrees(mapId, worldBox))
        {
            // If layers are not passed, then return all layers
            layers ??= [.. treeComp.Trees.Keys];
            foreach (int i in layers)
            {
                // Don't call GetOrCreateLayer as we are querying existing layers
                if (treeComp.Trees.TryGetValue(i, out var treeLayer))
                {
                    yield return (tree, treeComp, treeLayer, i);
                }
            }
        }
    }

    #endregion
}
#endregion
