using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;

namespace Robust.Shared.ComponentTrees;

/// <summary>
/// This system invokes movement events for use by <see cref="ComponentTreeSystem{TTreeComp,TComp}"/>.
/// It also handles the tracking of the transform hierarchy for recursive trees, i.e., any trees that need to be
/// updated not just when an entity moves, but if it or any of its parents move.
/// </summary>
internal sealed class RecursiveMoveSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private EntityQuery<MapComponent> _mapQuery;
    private EntityQuery<MapGridComponent> _gridQuery;
    private readonly Dictionary<EntityUid, EntityTree> _trees = new();
    // TODO TREE should this be a component?
    // Maybe, but this is probably faster for move event handling

    public delegate void RecursiveMoveHandler(ReadOnlySpan<EntityUid> entities);
    public delegate void MoveHandler(EntityUid uid, TransformComponent xform);
    public event RecursiveMoveHandler? OnRecursiveMove;
    public event MoveHandler? OnCompMoved;

    private bool _subscribed;

    public override void Initialize()
    {
        base.Initialize();
        _gridQuery = GetEntityQuery<MapGridComponent>();
        _mapQuery = GetEntityQuery<MapComponent>();
        _transform.OnBeforeMoveEvent += OnMove;
    }

    public override void Shutdown()
    {
        _transform.OnBeforeMoveEvent -= OnMove;
        EntityManager.BeforeEntityTerminating -= OnTerminating;
        OnRecursiveMove = null;
        OnCompMoved = null;
        _subscribed = false;
    }

    internal void AddSubscription()
    {
        if (_subscribed)
            return;

        _subscribed = true;
        EntityManager.BeforeEntityTerminating += OnTerminating;
        SubscribeLocalEvent<EntParentChangedMessage>(OnParentChange);
    }

    private void OnTerminating(ref EntityTerminatingEvent ev)
    {
        var flags = ev.Entity.Comp.Flags;
        ev.Entity.Comp.Flags &= ~(MetaDataFlags.RecursiveCompTree | MetaDataFlags.CompTree);
        if ((flags & MetaDataFlags.RecursiveCompTree) != 0)
            DeleteNode(ev.Entity.Owner);
    }

    private void OnMove(ref MoveEvent args)
    {
        if (args.Component.MapUid == args.Sender || args.Component.GridUid == args.Sender)
            return;

        DebugTools.Assert(!_mapQuery.HasComp(args.Sender));
        DebugTools.Assert(!_gridQuery.HasComp(args.Sender));

        var flags = args.Entity.Comp2.Flags;
        if ((flags & MetaDataFlags.CompTree) == MetaDataFlags.CompTree)
        {
            // Entity has at least one component associated with a either a non-recursive component lookup tree
            OnCompMoved?.Invoke(args.Sender, args.Entity);
        }

        if ((flags & MetaDataFlags.RecursiveCompTree) == 0)
            return;

        // This entity or one of its children has a component associated with a recursive tree.
        if (!_trees.TryGetValue(args.Sender, out var node))
        {
            DebugTools.Assert($"Entity {ToPrettyString(args.Sender)} has flag without tree?");
            return;
        }

        var span = node.Flatten();
        DebugTools.Assert(!node.Empty);
        DebugTools.Assert(span.Length > 0);
        OnRecursiveMove?.Invoke(span);
    }

    private void OnParentChange(ref EntParentChangedMessage ev)
    {
        if ((ev.Metadata.Flags & MetaDataFlags.RecursiveCompTree) == 0)
            return;

        // Check that this entity should even have the flag.
        Validate(ev.Entity);

        if (!_trees.TryGetValue(ev.Entity, out var node))
            return;

        // Remove the tree from it's old parent
        DebugTools.Assert(ev.OldParent == null || node.Parent == _trees.GetValueOrDefault(ev.OldParent.Value));
        if (node.Parent is { } parent)
        {
            node.Orphan();
            DeleteEmptyNode(parent);
        }

        if (ev.Transform.ParentUid is not {Valid: true} newParent)
            return;

        if (ev.Transform.GridUid == newParent || ev.Transform.MapUid == newParent)
            return;

        var newTree = GetTreeOrNull(newParent);
        newTree?.AddChild(node);
    }

    /// <summary>
    /// Gets or creates a new <see cref="EntityTree"/> associated with the given entity. If the entity is a map or grid
    /// this will instead return null.
    /// </summary>
    private EntityTree? GetTreeOrNull(EntityUid uid, bool onAdd = false)
    {
        DebugTools.Assert(!_gridQuery.HasComp(uid));
        DebugTools.Assert(!_mapQuery.HasComp(uid));

        ref var tree = ref CollectionsMarshal.GetValueRefOrAddDefault(_trees, uid, out var existing);
        if (existing)
        {
            Validate(uid);
            return tree;
        }

        // TODO TREE POOL
        tree = new(uid);
        DebugTools.Assert(onAdd || !HasRecursiveComponent(uid));
        DebugTools.Assert(!MetaData(uid).Flags.HasFlag(MetaDataFlags.RecursiveCompTree));
        MetaData(uid).Flags |= MetaDataFlags.RecursiveCompTree;

        // Add ourselves to our parent.
        var xform = Transform(uid);
        var parent = xform.ParentUid;
        if (!parent.IsValid() || xform.GridUid == parent || xform.MapUid == parent)
            return tree;

        var parentTree = GetTreeOrNull(parent);
        parentTree?.AddChild(tree);
        return tree;
    }

    /// <summary>
    /// Check whether the entity has any running component that needs to be stored in a recursive component tree.
    /// </summary>
    private bool HasRecursiveComponent(EntityUid uid)
    {
        if (TerminatingOrDeleted(uid))
            return false;

        // TODO TREE
        // There's probably a better way of doing this?
        // Maybe store compidx values in an array?
        // then again, outside of debug asserts its only used on component removal.
        var ev = new HasRecursiveTreeCompEvent();
        RaiseLocalEvent(uid, ref ev);
        return ev.Result;
    }

    public void OnCompAdded(EntityUid uid)
    {
        DebugTools.Assert(HasRecursiveComponent(uid));
        var tree = GetTreeOrNull(uid, onAdd: true);
        DebugTools.AssertNotNull(tree);

        if (tree != null)
            tree.Include = true;
    }

    public void OnCompRemoved(EntityUid uid)
    {
        if (!_trees.TryGetValue(uid, out var node))
            return;

        node.Include = HasRecursiveComponent(uid);
        DeleteEmptyNode(node);
    }

    /// <summary>
    /// Remove any empty nodes, recursively checking if the node's parent is empty.
    /// </summary>
    private void DeleteEmptyNode(EntityTree node)
    {
        while (node.Empty)
        {
            DebugTools.AssertEqual(node, _trees[node.Uid]);
            _trees.Remove(node.Uid);

            if (TryComp(node.Uid, out MetaDataComponent? meta))
                meta.Flags &= ~MetaDataFlags.RecursiveCompTree;

            if (node.Parent is not { } parent)
                return;

            node.Orphan();
            node = parent;
        }

        Validate(node.Uid);
    }

    private void DeleteNode(EntityUid uid)
    {
        if (!_trees.Remove(uid, out var node))
            return;

        DebugTools.Assert(!HasRecursiveComponent(uid));

        if (TryComp(node.Uid, out MetaDataComponent? meta))
            meta.Flags &= ~MetaDataFlags.RecursiveCompTree;

        foreach (var child in node.Children.Span)
        {
            child.Parent = null;
        }

        if (node.Parent is not { } parent)
            return;

        node.Orphan();
        DeleteEmptyNode(parent);
    }

    [Conditional("DEBUG")]
    private void Validate(EntityUid uid)
    {
        if (!_trees.TryGetValue(uid, out var node))
        {
            DebugTools.Assert($"{ToPrettyString(uid)} has no branch");
            return;
        }

        DebugTools.Assert(MetaData(uid).Flags.HasFlag(MetaDataFlags.RecursiveCompTree),
            $"{ToPrettyString(uid)} has no flag");
        DebugTools.AssertEqual(node.Include, HasRecursiveComponent(uid), $"{ToPrettyString(uid)} incorrect include");
        if (!node.Include)
            DebugTools.Assert(node.Children.Count > 0);
    }
}

[ByRefEvent]
internal struct HasRecursiveTreeCompEvent
{
    public bool Result;
}
