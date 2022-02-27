using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Robust.Server.GameStates;

public sealed class RobustTree<T> where T : notnull
{
    private Dictionary<T, TreeNode> _nodeIndex = new();

    private Dictionary<T, T> _parents = new();
    private HashSet<T> _rootNodes = new();

    private Func<HashSet<TreeNode>> _setProvider;
    private Action<HashSet<TreeNode>> _setConsumer;

    public RobustTree(Func<HashSet<TreeNode>>? setProvider = null, Action<HashSet<TreeNode>>? setConsumer = null)
    {
        _setProvider = setProvider ?? (static () => new());
        _setConsumer = setConsumer ?? (static (_) => {});
    }

    public void Clear()
    {
        // TODO: This is hella expensive
        foreach (var value in _nodeIndex.Values)
        {
            if(value.Children != null)
                _setConsumer(value.Children);
        }
        _nodeIndex.Clear();
        _parents.Clear();
        _rootNodes.Clear();
    }

    public void Remove(T value, bool mend = false)
    {
        if (!_nodeIndex.TryGetValue(value, out var node))
            throw new InvalidOperationException("Node doesnt exist.");


        if (_rootNodes.Contains(value))
        {
            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    _parents.Remove(child.Value);
                    _rootNodes.Add(child.Value);
                }
                _setConsumer(node.Children);
            }
            _rootNodes.Remove(value);
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
                        _parents[child.Value] = parent;
                        var children = _nodeIndex[parent].Children;
                        if (children == null)
                        {
                            children = _setProvider();
                            _nodeIndex[parent] = _nodeIndex[parent].WithChildren(children);
                        }
                        children.Add(child);
                    }
                    else
                    {
                        _parents.Remove(child.Value);
                        _rootNodes.Add(child.Value);
                    }
                }

                _setConsumer(node.Children);
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
            if(!_rootNodes.Contains(rootNode))
                throw new InvalidOperationException("Node already exists as non-root node.");
            return node;
        }

        node = new TreeNode(rootNode);
        _nodeIndex.Add(rootNode, node);
        _rootNodes.Add(rootNode);
        return node;
    }

    public TreeNode Set(T child, T parent)
    {
        if (!_nodeIndex.TryGetValue(parent, out var parentNode))
            parentNode = Set(parent);

        if (parentNode.Children == null)
        {
            _nodeIndex[parent] = parentNode = parentNode.WithChildren(_setProvider());
        }

        if (_nodeIndex.TryGetValue(child, out var existingNode))
        {
            if (_rootNodes.Contains(child))
            {
                parentNode.Children!.Add(existingNode);
                _rootNodes.Remove(child);
                _parents.Add(child, parent);
                return existingNode;
            }

            if (!_parents.TryGetValue(child, out var previousParent) || _nodeIndex.TryGetValue(previousParent, out var previousParentNode))
                throw new InvalidOperationException("Could not find old parent for non-root node.");

            previousParentNode.Children?.Remove(existingNode);
            parentNode.Children!.Add(existingNode);
            _parents[child] = parent;
            return existingNode;
        }

        existingNode = new TreeNode(child);
        _nodeIndex.Add(child, existingNode);
        parentNode.Children!.Add(existingNode);
        _parents.Add(child, parent);
        return existingNode;
    }

    // todo paul optimize this maybe as its basically all this is used for.
    public HashSet<TreeNode> GetRootNodes()
    {
        var nodes = _setProvider();
        foreach (var node in _rootNodes)
        {
            nodes.Add(_nodeIndex[node]);
        }

        return nodes;
    }

    public void ReturnRootNodes(HashSet<TreeNode> rootNodes)
    {
        _setConsumer(rootNodes);
    }

    public readonly struct TreeNode : IEquatable<TreeNode>
    {
        public readonly T Value;
        public readonly HashSet<TreeNode>? Children;

        public TreeNode(T value, HashSet<TreeNode>? children = null)
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

        public TreeNode WithChildren(HashSet<TreeNode> children)
        {
            return new TreeNode(Value, children);
        }
    }
}
