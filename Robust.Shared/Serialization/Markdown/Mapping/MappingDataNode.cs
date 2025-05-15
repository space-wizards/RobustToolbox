using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.InteropServices;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Utility;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Serialization.Markdown.Mapping
{
    public sealed class MappingDataNode : DataNode<MappingDataNode>, IDictionary<string, DataNode>
    {
        private readonly Dictionary<string, DataNode> _children;
        private readonly List<KeyValuePair<string,DataNode>> _list;

        /// <summary>
        /// ValueDataNodes associated with each key. This is used for yaml validation / error reporting.
        /// I.e., if a key is meant to be an EntityPrototype ID, we want to print an error that points to the
        /// corresponding yaml lines.
        /// </summary>
        private IReadOnlyDictionary<string, ValueDataNode>? _keyNodes;
        // TODO avoid populating this unless we are running the yaml linter?

        public override bool IsEmpty => _children.Count == 0;
        public int Count => _children.Count;
        public bool IsReadOnly => false;
        public IReadOnlyDictionary<string, DataNode> Children => _children;

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
            var keyNodes = new Dictionary<string, ValueDataNode>(mapping.Children.Count);
            foreach (var (keyNode, val) in mapping.Children)
            {
                if (keyNode is not YamlScalarNode scalarNode)
                    throw new NotSupportedException("Mapping data nodes must have a scalar keys");

                var valueNode = new ValueDataNode(scalarNode);
                Add(valueNode.Value, val.ToDataNode());
                keyNodes.Add(valueNode.Value, valueNode);
            }

            _keyNodes = keyNodes;
            Tag = mapping.Tag.IsEmpty ? null : mapping.Tag.Value;
        }

        public MappingDataNode(Dictionary<string, DataNode> nodes) : base(NodeMark.Invalid, NodeMark.Invalid)
        {
            _children = new(nodes);
            _list = new(_children);
        }

        public KeyValuePair<string, DataNode> this[int key] => _list[key];

        public MappingDataNode Add(string key, DataNode node)
        {
            _children.Add(key, node);
            _list.Add(new(key, node));
            return this;
        }

        public DataNode this[string key]
        {
            get => _children[key];
            set
            {
                if (_children.TryAdd(key, value))
                {
                    _list.Add(new(key, value));
                    return;
                }

                var index = IndexOf(key);
                if (index == -1)
                    throw new Exception("Key exists in Children, but not list?");

                _list[index] = new(key, value);
                _children[key] = value;
            }
        }

        public int IndexOf(string key)
        {
            // TODO MappingDataNode
            // Consider having a Dictionary<string,int> for faster lookups?
            // IndexOf() gets called in Remove(), which itself gets called frequently (e.g., per serialized component,
            // per entity, when loading a map.
            //
            // Then again, if most mappings only contain 1-4 entries, this list search is comparable in speed, reduces
            // allocations, and makes adding/inserting entries faster.

            for (var index = 0; index < _list.Count; index++)
            {
                if (_list[index].Key == key)
                    return index;
            }

            return -1;
        }

        void IDictionary<string, DataNode>.Add(string key, DataNode value) => Add(key, value);

        public bool ContainsKey(string key) => _children.ContainsKey(key);

        bool IDictionary<string, DataNode>.Remove(string key)
            => ((IDictionary<string, DataNode>)this).Remove(key);

        public bool TryGetValue(string key, [NotNullWhen(true)] out DataNode? value)
            => TryGet(key, out value);

        // TODO consider changing these to unsorted collections
        // I.e., just redirect to _children.Keys to avoid hidden linq & allocations.
        public ICollection<string> Keys => _list.Select(x => x.Key).ToArray();
        public ICollection<DataNode> Values => _list.Select(x => x.Value).ToArray();

        public DataNode Get(string key)
        {
            return _children[key];
        }

        public T Get<T>(string key) where T : DataNode
        {
            return (T) Get(key);
        }

        public bool TryGet(string key, [NotNullWhen(true)] out DataNode? node)
        {
            return _children.TryGetValue(key, out node);
        }

        public bool TryGet<T>(string key, [NotNullWhen(true)] out T? node) where T : DataNode
        {
            node = null;
            if (!TryGet(key, out var rawNode) || rawNode is not T castNode)
                return false;
            node = castNode;
            return true;
        }

        public bool Has(string key)
        {
            return _children.ContainsKey(key);
        }

        public bool Remove(string key)
        {
            if (!_children.Remove(key))
                return false;

            var index = IndexOf(key);
            if (index == -1)
                throw new Exception("Key exists in Children, but not list?");

            _list.RemoveAt(index);
            return true;
        }

        public T Cast<T>(string key) where T : DataNode
        {
            return (T) this[key];
        }

        public YamlMappingNode ToYaml()
        {
            var mapping = new YamlMappingNode();

            foreach (var (key, val) in _list)
            {
                YamlScalarNode yamlKeyNode;
                if (_keyNodes != null && _keyNodes.TryGetValue(key, out var keyNode))
                {
                    yamlKeyNode = (YamlScalarNode)keyNode;
                }
                else
                {
                    // This is matches the ValueDataNode -> YamlScalarNode cast operator
                    yamlKeyNode = new(key)
                    {
                        Style = ValueDataNode.IsNullLiteral(key) || string.IsNullOrWhiteSpace(key)
                            ? ScalarStyle.DoubleQuoted
                            : ScalarStyle.Any
                    };
                }

                mapping.Add(yamlKeyNode, val.ToYamlNode());
            }

            mapping.Tag = Tag;

            return mapping;
        }

        public ValueDataNode GetKeyNode(string key)
        {
            return _keyNodes?.GetValueOrDefault(key) ?? new ValueDataNode(key);
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
                    Add(key, val.Copy());
                }
            }
        }

        public void InsertAt(int index, string key, DataNode value)
        {
            if (index > _list.Count || index < 0)
                throw new ArgumentOutOfRangeException();

            if (!_children.TryAdd(key, value))
                throw new InvalidOperationException($"Already contains key {key}");

            _list.Insert(index, new(key, value));
        }

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
                newMapping.Add(key, val.Copy());
            }

            newMapping._keyNodes = _keyNodes;
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
                if (!node._children.TryGetValue(key, out var otherVal))
                {
                    mappingNode.Add(key, val.Copy());
                }
                else if (val.Except(otherVal) is { } newValue)
                {
                    mappingNode.Add(key, newValue);
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
                if (!node._children.TryGetValue(key, out var otherVal) || !val.Equals(otherVal))
                    mappingNode.Add(key, val.Copy());
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

            if (Tag != other.Tag)
                return false;

            foreach (var (key, otherValue) in other)
            {
                if (!_children.TryGetValue(key, out var ownValue)
                    || !otherValue.Equals(ownValue))
                {
                    return false;
                }
            }

            return true;
        }

        public List<KeyValuePair<string, DataNode>>.Enumerator GetEnumerator() => _list.GetEnumerator();
        IEnumerator<KeyValuePair<string, DataNode>> IEnumerable<KeyValuePair<string, DataNode>>.GetEnumerator() =>
            _list.GetEnumerator();

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

        public void Add(KeyValuePair<string, DataNode> item) => Add(item.Key, item.Value);

        public void Clear()
        {
            _children.Clear();
            _list.Clear();
        }

        public bool Contains(KeyValuePair<string, DataNode> item) => _children.ContainsKey(item.Key);

        [Obsolete("Use SerializationManager.PushComposition()")]
        public override MappingDataNode PushInheritance(MappingDataNode node)
        {
            var newNode = Copy();
            foreach (var (key, val) in node)
            {
                if (_children.ContainsKey(key))
                    continue;

                newNode.Remove(key);
                newNode.Add(key, val.Copy());
            }

            return newNode;
        }

        public void CopyTo(KeyValuePair<string, DataNode>[] array, int arrayIndex)
            => _list.CopyTo(array, arrayIndex);

        public bool Remove(KeyValuePair<string, DataNode> item)
            => ((IDictionary<string, DataNode>) this).Remove(item.Key);

        public bool TryAdd(string key, DataNode value)
        {
            if (!_children.TryAdd(key, value))
                return false;

            _list.Add(new(key, value));
            return true;
        }

        public bool TryAddCopy(string key, DataNode value)
        {
            ref var entry = ref CollectionsMarshal.GetValueRefOrAddDefault(_children, key, out var exists);
            if (exists)
                return false;

            entry = value.Copy();
            _list.Add(new(key, entry));
            return true;
        }

        // These methods are probably fine to keep around as helper methods, but are currently marked as obsolete
        // so that people don't uneccesarily allocate a ValueDataNode. I.e., to prevent people from using code like
        // mapping.TryGet(new ValueDataNode("key"), ...)
        #region ValueDataNode Helpers

        [Obsolete("Use string keys instead of ValueDataNode")]
        public bool TryGet(ValueDataNode key, [NotNullWhen(true)] out DataNode? value)
            => TryGet(key.Value, out value);

        [Obsolete("Use string keys instead of ValueDataNode")]
        public DataNode this[ValueDataNode key]
        {
            get => this[key.Value];
            set => this[key.Value] = value;
        }

        [Obsolete("Use string keys instead of ValueDataNode")]
        public bool TryGetValue(ValueDataNode key, [NotNullWhen(true)] out DataNode? value)
            => TryGet(key.Value, out value);

        [Obsolete("Use string keys instead of ValueDataNode")]
        public bool TryGet<T>(ValueDataNode key, [NotNullWhen(true)] out T? node) where T : DataNode
            => TryGet(key.Value, out node);

        [Obsolete("Use string keys instead of ValueDataNode")]
        public bool Has(ValueDataNode key) => Has(key.Value);

        [Obsolete("Use string keys instead of ValueDataNode")]
        public T Cast<T>(ValueDataNode key) where T : DataNode => Cast<T>(key.Value);

        [Obsolete("Use string keys instead of ValueDataNode")]
        public void Add(KeyValuePair<ValueDataNode, DataNode> item) => Add(item.Key, item.Value);

        [Obsolete("Use string keys instead of ValueDataNode")]
        public MappingDataNode Add(ValueDataNode key, DataNode node) => Add(key.Value, node);

        [Obsolete("Use string keys instead of ValueDataNode")]
        public void InsertAt(int index, ValueDataNode key, DataNode value) => InsertAt(index, key.Value, value);

        [Obsolete("Use string keys instead of ValueDataNode")]
        public bool Contains(KeyValuePair<ValueDataNode, DataNode> item) => _children.ContainsKey(item.Key.Value);

        [Obsolete("Use string keys instead of ValueDataNode")]
        public bool Remove(ValueDataNode key) => Remove(key.Value);

        #endregion
    }
}
