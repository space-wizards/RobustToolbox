using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.Utility;

namespace Robust.Shared.Prototypes;

public sealed class MultiRootInheritanceGraph<T> where T : notnull
{
    private readonly HashSet<T> _rootNodes = new();
    private readonly Dictionary<T, HashSet<T>> _edges = new();
    private readonly Dictionary<T, T[]> _parents = new();

    public bool Add(T id) => _rootNodes.Add(id);

    public IReadOnlySet<T> RootNodes => _rootNodes;

    public IReadOnlySet<T>? GetChildren(T id) => _edges.GetValueOrDefault(id);

    public bool TryGetChildren(T id, [NotNullWhen(true)] out IReadOnlySet<T>? set)
    {
        set = GetChildren(id);
        return set != null;
    }

    public T[]? GetParents(T id) => _parents.GetValueOrDefault(id);
    public int GetParentsCount(T id) => _parents.GetValueOrDefault(id)?.Length ?? 0;

    public bool TryGetParents(T id, [NotNullWhen(true)] out T[]? parents)
    {
        parents = GetParents(id);
        return parents != null;
    }

    public void Add(T id, params T[] parents)
    {
        //check for circular inheritance
        foreach (var parent in parents)
        {
            if (EqualityComparer<T>.Default.Equals(parent, id))
                throw new InvalidOperationException($"Self Inheritance detected for id \"{id}\"!");

            var parentsL1 = GetParents(parent);
            if(parentsL1 == null) continue;

            var queue = new Queue<T>(parentsL1);
            while (queue.TryDequeue(out var parentL1))
            {
                if (EqualityComparer<T>.Default.Equals(parentL1,id))
                    throw new InvalidOperationException(
                        $"Circular Inheritance detected for id \"{id}\" and parent \"{parent}\"");
                var parentsL2 = GetParents(parentL1);
                if (parentsL2 != null)
                {
                    foreach (var parentL3 in parentsL2)
                    {
                        queue.Enqueue(parentL3);
                    }
                }
            }
        }

        _rootNodes.Remove(id);

        foreach (var parent in parents)
        {
            var edges = _edges.GetOrNew(parent);
            edges.Add(id);
            _parents[id] = parents;

            if (!_parents.ContainsKey(parent))
                _rootNodes.Add(parent);
        }
    }

    public bool Remove(T id, bool force = false)
    {
        if (!force && _edges.ContainsKey(id)) throw new InvalidOperationException("Cannot remove node that has remaining children");

        var result = _rootNodes.Remove(id);

        if (_parents.TryGetValue(id, out var parents))
        {
            result = true;

            foreach (var parent in parents)
            {
                _edges[parent].Remove(id);
            }

            _parents.Remove(id);
        }

        if (force)
        {
            if (_edges.TryGetValue(id, out var children))
            {
                foreach (var child in children)
                {
                    var childParents = _parents[child];
                    var newParents = new T[childParents.Length - 1];
                    var i = 0;
                    foreach (var childParent in childParents)
                    {
                        if(Equals(childParent, id)) continue;
                        newParents[i++] = childParent;
                    }

                    if (newParents.Length == 0)
                    {
                        _rootNodes.Add(child);
                        _parents.Remove(child);
                    }
                    else
                    {
                        _parents[child] = newParents;
                    }
                }
            }
        }

        return result;
    }
}
