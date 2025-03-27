using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Serialization.Markdown.Mapping
{
    public sealed class MappingDataNode : DataNode<MappingDataNode>, IDictionary<DataNode, DataNode>
    {
        // To fetch nodes by key name with YAML, we NEED a YamlScalarNode.
        // We use a thread local one to avoid allocating one every fetch, since we just replace the inner value.
        // Obviously thread local to avoid threading issues.
        private static readonly ThreadLocal<ValueDataNode> FetchNode =
            new(() => new ValueDataNode(""));

        private readonly Dictionary<DataNode, DataNode> _children;
        private readonly List<KeyValuePair<DataNode,DataNode>> _list;

        public IReadOnlyDictionary<DataNode, DataNode> Children => _children;

        public MappingDataNode() : base(NodeMark.Invalid, NodeMark.Invalid)
        {
            _children = new();
            _list = new();
        }

        public MappingDataNode(int size) : base(NodeMark.Invalid, NodeMark.Invalid)
        {
            _children = new(size);
            _list = new(size);
        }

        public MappingDataNode(YamlMappingNode mapping) : base(mapping.Start, mapping.End)
        {
            _children = new(mapping.Children.Count);
            _list = new(mapping.Children.Count);
            foreach (var (key, val) in mapping.Children)
            {
                Add(key.ToDataNode(), val.ToDataNode());
            }

            Tag = mapping.Tag.IsEmpty ? null : mapping.Tag.Value;
        }

        public MappingDataNode(Dictionary<DataNode, DataNode> nodes) : base(NodeMark.Invalid, NodeMark.Invalid)
        {
            _children = new(nodes.Count);
            _list = new(nodes.Count);
            foreach (var (key, val) in nodes)
            {
                Add(key, val);
            }
        }

        public KeyValuePair<DataNode, DataNode> this[int key] => _list[key];

        public DataNode this[string index]
        {
            get => Get(index);
            set => Add(index, value);
        }

        public MappingDataNode Add(string key, DataNode node)
        {
            Add(new ValueDataNode(key), node);
            return this;
        }

        private static ValueDataNode GetFetchNode(string key)
        {
            var node = FetchNode.Value!;
            node.Value = key;
            return node;
        }

        public MappingDataNode Add(DataNode key, DataNode node)
        {
            _children.Add(key, node);
            _list.Add(new(key, node));
            return this;
        }

        public DataNode this[DataNode key]
        {
            get => _children[key];
            set
            {
                if (_children.TryAdd(key, value))
                {
                    _list.Add(new( key, value));
                    return;
                }

                var i = _list.IndexOf(new(key, _children[key]));
                _list[i] = new(key, value);
                _children[key] = value;
            }
        }

        void IDictionary<DataNode, DataNode>.Add(DataNode key, DataNode value) => Add(key, value);

        public bool ContainsKey(DataNode key) => _children.ContainsKey(key);

        bool IDictionary<DataNode, DataNode>.Remove(DataNode key)
            => ((IDictionary<DataNode, DataNode>)this).Remove(key);

        public bool TryGetValue(DataNode key, [NotNullWhen(true)] out DataNode? value) => TryGet(key, out value);

        public ICollection<DataNode> Keys => _list.Select(x => x.Key).ToArray();
        public ICollection<DataNode> Values => _list.Select(x => x.Value).ToArray();

        public DataNode Get(DataNode key)
        {
            return _children[key];
        }

        public T Get<T>(DataNode key) where T : DataNode
        {
            return (T) Get(key);
        }

        public DataNode Get(string key)
        {
            return Get(GetFetchNode(key));
        }

        public T Get<T>(string key) where T : DataNode
        {
            return Get<T>(GetFetchNode(key));
        }

        public bool TryGet(DataNode key, [NotNullWhen(true)] out DataNode? node)
        {
            if (_children.TryGetValue(key, out node))
            {
                return true;
            }

            node = null;
            return false;
        }

        public bool TryGet<T>(DataNode key, [NotNullWhen(true)] out T? node) where T : DataNode
        {
            node = null;
            if (!TryGet(key, out var rawNode) || rawNode is not T castNode)
                return false;
            node = castNode;
            return true;
        }

        public bool TryGet(string key, [NotNullWhen(true)] out DataNode? node)
        {
            return TryGet(GetFetchNode(key), out node);
        }

        public bool TryGet<T>(string key, [NotNullWhen(true)] out T? node) where T : DataNode
        {
            return TryGet(GetFetchNode(key), out node);
        }

        public bool Has(DataNode key)
        {
            return _children.ContainsKey(key);
        }

        public bool Has(string key)
        {
            return Has(GetFetchNode(key));
        }

        public MappingDataNode Remove(DataNode key)
        {
            if (_children.Remove(key, out var val))
                _list.Remove(new(key, val));
            return this;
        }

        public MappingDataNode Remove(string key)
        {
            return Remove(GetFetchNode(key));
        }

        public T Cast<T>(string index) where T : DataNode
        {
            return (T) this[index];
        }

        public YamlMappingNode ToYaml()
        {
            var mapping = new YamlMappingNode();

            foreach (var (key, val) in _list)
            {
                mapping.Add(key.ToYamlNode(), val.ToYamlNode());
            }

            mapping.Tag = Tag;

            return mapping;
        }

        public MappingDataNode Merge(MappingDataNode otherMapping)
        {
            var newMapping = Copy();
            newMapping.Insert(otherMapping);

            // TODO Serialization: should prob make this smarter
            newMapping.Tag = Tag;
            newMapping.Start = Start;
            newMapping.End = End;

            return newMapping;
        }

        public void Insert(MappingDataNode otherMapping, bool skipDuplicates = false)
        {
            foreach (var (key, val) in otherMapping.Children)
            {
                if (!skipDuplicates || !Has(key))
                {
                    // Intentionally raises an ArgumentException
                    Add(key.Copy(), val.Copy());
                }
            }
        }

        public void InsertAt(int index, DataNode key, DataNode value)
        {
            if (index > _list.Count || index < 0)
                throw new ArgumentOutOfRangeException();

            if (!_children.TryAdd(key, value))
                throw new InvalidOperationException($"Already contains key {key}");

            _list.Insert(index, new(key, value));
        }

        public void InsertAt(int index, string key, DataNode value)
        {
            if (index > _list.Count || index < 0)
                throw new ArgumentOutOfRangeException();

            var k = new ValueDataNode(key);
            if (!_children.TryAdd(k, value))
                throw new InvalidOperationException($"Already contains key {key}");

            _list.Insert(index, new(k, value));
        }

        public override bool IsEmpty => _children.Count == 0;

        public override MappingDataNode Copy()
        {
            var newMapping = new MappingDataNode(_children.Count)
            {
                Tag = Tag,
                Start = Start,
                End = End
            };

            foreach (var (key, val) in _list)
            {
                newMapping.Add(key.Copy(), val.Copy());
            }

            return newMapping;
        }

        /// <summary>
        /// Variant of <see cref="Copy"/> that doesn't clone the keys or values.
        /// </summary>
        public MappingDataNode ShallowClone()
        {
            var newMapping = new MappingDataNode(_children.Count)
            {
                Tag = Tag,
                Start = Start,
                End = End
            };

            foreach (var (key, val) in _list)
            {
                newMapping.Add(key, val);
            }

            return newMapping;
        }

        /// <summary>
        ///     Variant of <see cref="Except(MappingDataNode)"/> that will recursively call except rather than only checking equality.
        /// </summary>
        public MappingDataNode? RecursiveExcept(MappingDataNode node)
        {
            var mappingNode = new MappingDataNode()
            {
                Tag = Tag,
                Start = Start,
                End = End
            };

            foreach (var (key, val) in _list)
            {
                var other = node._list.FirstOrNull(p => p.Key.Equals(key));
                if (other == null)
                {
                    mappingNode.Add(key.Copy(), val.Copy());
                }
                else
                {
                    // We recursively call except on the values and keep only the differences.
                    var newValue = val.Except(other.Value.Value);
                    if (newValue == null) continue;
                    mappingNode.Add(key.Copy(), newValue);
                }
            }

            return mappingNode._children.Count == 0 ? null : mappingNode;
        }

        public override MappingDataNode? Except(MappingDataNode node)
        {
            var mappingNode = new MappingDataNode()
            {
                Tag = Tag,
                Start = Start,
                End = End
            };

            foreach (var (key, val) in _list)
            {
                var other = node._list.FirstOrNull(p => p.Key.Equals(key));

                if (other == null)
                {
                    mappingNode.Add(key.Copy(), val.Copy());
                }
                else
                {
                    // We only keep the entry if the values are not equal
                    if (!val.Equals(other.Value.Value))
                        mappingNode.Add(key.Copy(), val.Copy());
                }
            }

            return mappingNode._children.Count == 0 ? null : mappingNode;
        }

        /// <summary>
        /// Returns true if there are any nodes on this node that aren't in the other node.
        /// </summary>
        [Pure]
        public bool AnyExcept(MappingDataNode node)
        {
            foreach (var (key, val) in _list)
            {
                var other = node._list.FirstOrNull(p => p.Key.Equals(key));

                if (other == null)
                {
                    return true;
                }

                // We only keep the entry if the values are not equal
                if (!val.Equals(other.Value.Value))
                    return true;
            }

            return false;
        }

        public override bool Equals(object? obj)
        {
            if (obj is not MappingDataNode other)
                return false;

            if (_children.Count != other._children.Count)
                return false;

            foreach (var (key, otherValue) in other)
            {
                if (!_children.TryGetValue(key, out var ownValue) ||
                    !otherValue.Equals(ownValue))
                {
                    return false;
                }
            }

            return Tag == other.Tag;
        }

        public override MappingDataNode PushInheritance(MappingDataNode node)
        {
            var newNode = Copy();
            foreach (var (key, val) in node)
            {
                if(_children.ContainsKey(key))
                    continue;

                newNode.Remove(key);
                newNode.Add(key.Copy(), val.Copy());
            }

            return newNode;
        }

        public IEnumerator<KeyValuePair<DataNode, DataNode>> GetEnumerator() => _list.GetEnumerator();

        public override int GetHashCode()
        {
            var code = new HashCode();
            foreach (var (key, value) in _children)
            {
                code.Add(key);
                code.Add(value);
            }

            return code.ToHashCode();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(KeyValuePair<DataNode, DataNode> item) => Add(item.Key, item.Value);

        public void Clear()
        {
            _children.Clear();
            _list.Clear();
        }

        public bool Contains(KeyValuePair<DataNode, DataNode> item) => _children.ContainsKey(item.Key);

        public void CopyTo(KeyValuePair<DataNode, DataNode>[] array, int arrayIndex)
            => _list.CopyTo(array, arrayIndex);

        public bool Remove(KeyValuePair<DataNode, DataNode> item)
            => ((IDictionary<DataNode, DataNode>)this).Remove(item.Key);

        public int Count => _children.Count;
        public bool IsReadOnly => false;

        public bool TryAdd(DataNode key, DataNode value)
        {
            if (!_children.TryAdd(key, value))
                return false;

            _list.Add(new(key, value));
            return true;
        }

        public bool TryAddCopy(DataNode key, DataNode value)
        {
            ref var entry = ref CollectionsMarshal.GetValueRefOrAddDefault(_children, key, out var exists);
            if (exists)
                return false;

            entry = value.Copy();
            _list.Add(new(key, entry));
            return true;
        }
    }
}
