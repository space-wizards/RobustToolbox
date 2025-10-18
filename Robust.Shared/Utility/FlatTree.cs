using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Robust.Shared.Collections;
using Robust.Shared.GameObjects;

namespace Robust.Shared.Utility;

// TODO TREE
// Maybe private to the recursive move system?
/// <summary>
/// Simple tree data structure that is intended to be updated infrequently and support fast enumeration via
/// <see cref="Flatten()"/>.
/// </summary>
internal sealed class EntityTree(EntityUid value)
{
    // TODO TREE name better
    // Originally I made a distinction between the "tree" and the tree's nodes
    // But there wasn't really any meaningfully distinction between a tree and a node with no parent.
    // But now I don't know what to call it.

    public ValueList<EntityTree> Children;
    public EntityUid Uid = value;
    public EntityTree? Parent;

    public bool Empty => !Include && Children.Count == 0;

    /// <summary>
    /// Whether this entity should be included when flattening the tree via <see cref="Flatten"/>
    /// </summary>
    public bool Include
    {
        get => _include;
        set
        {
            if (_include == value)
                return;

            _include = value;
            InvalidateFlatten();
        }
    }

    private bool _include;

    // Could be value list, but I can't help but assume that AsSpan and AddRange(Span) will be more optimized
    private  List<EntityUid>? _flatList;

    private int _version;
    private int _lastVersion = -1;
    private int _index = -1;

    /// <summary>
    /// Remove a node from its parent.
    /// </summary>
    public void Orphan()
    {
        if (Parent == null)
            return;

        // RemoveSwap, while updating the replacement's index
        var last = Parent.Children.Count - 1;
        var replacement = Parent.Children[_index] = Parent.Children[last];
        replacement._index = _index;
        Parent.Children.RemoveAt(last);
        Parent.InvalidateFlatten();
        _index = -1;
        Parent = null;
    }

    public void AddChild(EntityTree child)
    {
        child.Orphan();
        child._index = Children.Count;
        child.Parent = this;
        Children.Add(child);
        InvalidateFlatten();
    }

    public ReadOnlySpan<EntityUid> Flatten()
    {
        if (_lastVersion == _version)
            return CollectionsMarshal.AsSpan(_flatList);

        _lastVersion = _version;

        _flatList ??= new();
        _flatList.Clear();

        if (Include)
            _flatList.Add(Uid);

        foreach (var child in Children)
        {
            _flatList.AddRange(child.Flatten());
        }

        return CollectionsMarshal.AsSpan(_flatList);
    }

    private void InvalidateFlatten()
    {
        _version++;
        Parent?.InvalidateFlatten();
    }
}
