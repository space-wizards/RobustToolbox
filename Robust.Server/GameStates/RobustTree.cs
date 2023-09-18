using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Extensions.ObjectPool;
using Robust.Shared.Utility;

namespace Robust.Server.GameStates;

public sealed class RobustTree<T> where T : notnull
{
    private Dictionary<T, TreeNode> _nodeIndex = new();

    private Dictionary<T, T> _parents = new();
    public readonly HashSet<T> RootNodes = new();

    private ObjectPool<HashSet<T>> _pool;

    public RobustTree(ObjectPool<HashSet<T>>? pool = null)
    {
        _pool = pool ?? new DefaultObjectPool<HashSet<T>>(new SetPolicy<T>());
    }

    public void Clear()
    {
        foreach (var value in _nodeIndex.Values)
        {
            if(value.Children != null)
                _pool.Return(value.Children);
        }
        _nodeIndex.Clear();
        _parents.Clear();
        RootNodes.Clear();
    }

    public TreeNode this[T index] => _nodeIndex[index];

    public void Remove(T value, bool mend = false)
    {
        if (!_nodeIndex.TryGetValue(value, out var node))
            throw new InvalidOperationException("Node doesnt exist.");


        if (RootNodes.Contains(value))
        {
            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    _parents.Remove(child);
                    RootNodes.Add(child);
                }
                _pool.Return(node.Children);
            }
            RootNodes.Remove(value);
            _nodeIndex.Remove(value);
            return;
        }

        if (_parents.TryGetValue(value, out var parent))
        {
            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    if (mend)
                    {
                        _parents[child] = parent;
                        var children = _nodeIndex[parent].Children;
                        if (children == null)
                        {
                            children = _pool.Get();
                            _nodeIndex[parent] = _nodeIndex[parent].WithChildren(children);
                        }
                        children.Add(child);
                    }
                    else
                    {
                        _parents.Remove(child);
                        RootNodes.Add(child);
                    }
                }

                _pool.Return(node.Children);
            }
            _parents.Remove(value);
            _nodeIndex.Remove(value);
        }

        throw new InvalidOperationException("Node neither had a parent nor was a RootNode.");
    }

    public void Set(T rootNode)
    {
        ref var node = ref CollectionsMarshal.GetValueRefOrAddDefault(_nodeIndex, rootNode, out var exists);

        if (exists)
        {
            if(!RootNodes.Contains(rootNode))
                throw new InvalidOperationException("Node already exists as non-root node.");
            return;
        }

        node = new TreeNode(rootNode);
        if(!RootNodes.Add(rootNode))
            throw new InvalidOperationException("Non-existent node was already a root node?");
    }

    public void Set(T child, T parent)
    {
        // Code block for where parentNode is a valid ref
        {
            ref var parentNode = ref CollectionsMarshal.GetValueRefOrAddDefault(_nodeIndex, parent, out var parentExists);

            // If parent does not exist we make it a new root node.
            if (!parentExists)
            {
                parentNode = new TreeNode(parent);
                if (!RootNodes.Add(parent))
                {
                    _nodeIndex.Remove(parent);
                    throw new InvalidOperationException("Non-existent node was already a root node?");
                }
            }

            var children = parentNode.Children;
            if (children == null)
            {
                children = _pool.Get();
                parentNode = parentNode.WithChildren(children);
                DebugTools.AssertNotNull(_nodeIndex[parent].Children);
            }
            children.Add(child);
        }

        // No longer safe to access parentNode ref after this.

        ref var node = ref CollectionsMarshal.GetValueRefOrAddDefault(_nodeIndex, child, out var childExists);
        if (!childExists)
        {
            // This is the path that PVS should take 99% of the time.
            node = new TreeNode(child);
            _parents.Add(child, parent);
            return;
        }

        if (RootNodes.Remove(child))
        {
            DebugTools.Assert(!_parents.ContainsKey(child));
            _parents.Add(child, parent);
            return;
        }

        ref var parentEntry = ref CollectionsMarshal.GetValueRefOrAddDefault(_parents, child, out var previousParentExists);
        if (!previousParentExists || !_nodeIndex.TryGetValue(parentEntry!, out var previousParentNode))
        {
            parentEntry = parent;
            throw new InvalidOperationException("Could not find old parent for non-root node.");
        }

        previousParentNode.Children?.Remove(child);
        parentEntry = parent;
    }

    public readonly struct TreeNode : IEquatable<TreeNode>
    {
        public readonly T Value;
        public readonly HashSet<T>? Children;

        public TreeNode(T value, HashSet<T>? children = null)
        {
            Value = value;
            Children = children;
        }

        public bool Equals(TreeNode other)
        {
            return Value.Equals(other.Value) && Children?.Equals(other.Children) == true;
        }

        public override bool Equals(object? obj)
        {
            return obj is TreeNode other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value, Children);
        }

        public TreeNode WithChildren(HashSet<T> children)
        {
            return new TreeNode(Value, children);
        }
    }
}
