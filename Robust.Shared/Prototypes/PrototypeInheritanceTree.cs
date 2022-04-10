using System;
using System.Collections.Generic;

namespace Robust.Shared.Prototypes
{
    public sealed class PrototypeInheritanceTree
    {
        private Dictionary<string, HashSet<string>> _nodes = new();

        private Dictionary<string, HashSet<string>> _pendingParent = new();

        private HashSet<string> _baseNodes = new();

        private Dictionary<string, string> _parents = new();

        public IReadOnlySet<string> BaseNodes => _baseNodes;

        public IReadOnlySet<string> Children(string id)
        {
            if (!_nodes.ContainsKey(id))
                throw new ArgumentException($"ID {id} not present in InheritanceTree", nameof(id));
            return _nodes[id];
        }

        public string GetBaseNode(string id)
        {
            if (!_nodes.ContainsKey(id))
                throw new ArgumentException($"ID {id} not present in InheritanceTree", nameof(id));

            var parent = id;
            while (_parents.TryGetValue(parent, out var nextParent))
            {
                parent = nextParent;
            }

            return parent;
        }

        public string? GetParent(string id)
        {
            return _parents.GetValueOrDefault(id);
        }

        public void AddId(string id, string? parent, bool overwrite = false)
        {
            if (overwrite && HasId(id))
            {
                RemoveId(id);
            }

            if (_nodes.ContainsKey(id))
                throw new ArgumentException($"ID {id} already present in InheritanceTree", nameof(id));

            if (parent != null)
            {
                _parents.Add(id, parent);

                if (_nodes.TryGetValue(parent, out var parentsChildren))
                {
                    parentsChildren.Add(id);
                }
                else
                {
                    if (!_pendingParent.TryGetValue(parent, out _))
                    {
                        _pendingParent[parent] = new HashSet<string>();
                    }

                    _pendingParent[parent].Add(id);
                }

                //cycle detection
                var currentParent = parent;
                while (currentParent != null)
                {
                    if (currentParent == id)
                        throw new InvalidOperationException(
                            $"Cycle detected when trying to add id {id} with parent {parent}");
                    _parents.TryGetValue(currentParent, out currentParent);
                }
            }
            else
            {
                _baseNodes.Add(id);
            }

            if (!_pendingParent.TryGetValue(id, out var ourChildren))
                ourChildren = new HashSet<string>();

            _nodes.Add(id, ourChildren);
        }

        public bool HasId(string id)
        {
            return _nodes.ContainsKey(id);
        }

        public void RemoveId(string id)
        {
            if (!_nodes.ContainsKey(id))
                throw new ArgumentException($"ID {id} not present in InheritanceTree", nameof(id));

            _nodes.Remove(id);
            foreach (var (_, children) in _pendingParent)
            {
                children.Remove(id);
            }

            _baseNodes.Remove(id);
            _parents.Remove(id);
        }
    }
}
