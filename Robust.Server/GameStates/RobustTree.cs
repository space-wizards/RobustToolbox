using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.ObjectPool;

namespace Robust.Server.GameStates;

public sealed class RobustTree<T> where T : notnull
{
    private Dictionary<T, TreeNode> _nodeIndex = new();

    private Dictionary<T, T> _parents = new();
    public readonly HashSet<T> RootNodes = new();

    private ObjectPool<HashSet<T>> _pool;

    public RobustTree(ObjectPool<HashSet<T>>? pool = null)
    {
        _pool = pool ?? new DefaultObjectPool<HashSet<T>>(new PVSSystem.SetPolicy<T>());
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

    public TreeNode Set(T rootNode)
    {
        //root node, for now
        if (_nodeIndex.TryGetValue(rootNode, out var node))
        {
            if(!RootNodes.Contains(rootNode))
                throw new InvalidOperationException("Node already exists as non-root node.");
            return node;
        }

        node = new TreeNode(rootNode);
        _nodeIndex.Add(rootNode, node);
        RootNodes.Add(rootNode);
        return node;
    }

    public TreeNode Set(T child, T parent)
    {
        if (!_nodeIndex.TryGetValue(parent, out var parentNode))
            parentNode = Set(parent);

        if (parentNode.Children == null)
        {
            _nodeIndex[parent] = parentNode = parentNode.WithChildren(_pool.Get());
        }

        if (_nodeIndex.TryGetValue(child, out var existingNode))
        {
            if (RootNodes.Contains(child))
            {
                parentNode.Children!.Add(existingNode.Value);
                RootNodes.Remove(child);
                _parents.Add(child, parent);
                return existingNode;
            }

            if (!_parents.TryGetValue(child, out var previousParent) || _nodeIndex.TryGetValue(previousParent, out var previousParentNode))
                throw new InvalidOperationException("Could not find old parent for non-root node.");

            previousParentNode.Children?.Remove(existingNode.Value);
            parentNode.Children!.Add(existingNode.Value);
            _parents[child] = parent;
            return existingNode;
        }

        existingNode = new TreeNode(child);
        _nodeIndex.Add(child, existingNode);
        parentNode.Children!.Add(existingNode.Value);
        _parents.Add(child, parent);
        return existingNode;
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
