using System;
using System.Collections.Generic;

namespace Robust.Server.GameStates;

public sealed class RobustTree<T> where T : notnull
{
    private Dictionary<T, TreeNode> _nodeIndex = new();

    private Dictionary<T, T> _parents = new();
    private HashSet<T> _rootNodes = new();

    private Func<HashSet<TreeNode>> _setProvider;
    private Action<HashSet<TreeNode>> _setConsumer;

    public RobustTree() : this(static () => new HashSet<TreeNode>(), static (_) => {}) { }

    public RobustTree(Func<HashSet<TreeNode>> setProvider, Action<HashSet<TreeNode>> setConsumer)
    {
        _setProvider = setProvider;
        _setConsumer = setConsumer;
    }

    public void Remove(T value, bool mend = false)
    {
        if (!_nodeIndex.TryGetValue(value, out var node))
            throw new InvalidOperationException("Node doesnt exist.");

        if (_rootNodes.Contains(value))
        {
            foreach (var child in node.Children)
            {
                _parents.Remove(child.Value);
                _rootNodes.Add(child.Value);
            }
            _setConsumer(node.Children);
            _rootNodes.Remove(value);
            _nodeIndex.Remove(value);
            return;
        }

        if (_parents.TryGetValue(value, out var parent))
        {
            foreach (var child in node.Children)
            {
                if (mend)
                {
                    _parents[child.Value] = parent;
                    _nodeIndex[parent].Children.Add(child);
                }
                else
                {
                    _parents.Remove(child.Value);
                    _rootNodes.Add(child.Value);
                }
            }

            _setConsumer(node.Children);
            _parents.Remove(value);
            _nodeIndex.Remove(value);
        }

        throw new InvalidOperationException("Node neither had a parent nor was a RootNode.");
    }

    public TreeNode Set(T child, T? parent = default)
    {
        if (parent == null)
        {
            //root node, for now
            if (_nodeIndex.TryGetValue(child, out var node))
            {
                if(!_rootNodes.Contains(child))
                    throw new InvalidOperationException("Node already exists as non-root node.");
                return node;
            }

            node = new TreeNode(child, _setProvider());
            _nodeIndex.Add(child, node);
            _rootNodes.Add(child);
            return node;
        }

        if (!_nodeIndex.TryGetValue(parent, out var parentNode))
            parentNode = Set(parent);

        if (_nodeIndex.TryGetValue(child, out var existingNode))
        {
            if (_rootNodes.Contains(child))
            {
                parentNode.Children.Add(existingNode);
                _rootNodes.Remove(child);
                _parents.Add(child, parent);
                return existingNode;
            }

            if (!_parents.TryGetValue(child, out var previousParent) || _nodeIndex.TryGetValue(previousParent, out var previousParentNode))
                throw new InvalidOperationException("Could not find old parent for non-root node.");

            previousParentNode.Children.Remove(existingNode);
            parentNode.Children.Add(existingNode);
            _parents[child] = parent;
            return existingNode;
        }

        existingNode = new TreeNode(child, _setProvider());
        _nodeIndex.Add(child, existingNode);
        parentNode.Children.Add(existingNode);
        _parents.Add(child, parent);
        return existingNode;
    }

    public struct TreeNode
    {
        public readonly T Value;
        public readonly HashSet<TreeNode> Children;

        public TreeNode(T value, HashSet<TreeNode> children)
        {
            Value = value;
            Children = children;
        }
    }
}
