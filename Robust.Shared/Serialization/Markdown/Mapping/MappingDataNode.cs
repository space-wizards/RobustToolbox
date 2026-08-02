using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Utility;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Serialization.Markdown.Mapping
{
    /// <summary>
    /// A yaml mapping. Small mappings use one flat array of key/value pairs; larger mappings promote to a dictionary.
    /// </summary>
    public sealed class MappingDataNode : DataNode<MappingDataNode>, IDictionary<string, DataNode>, IReadOnlyDictionary<string, DataNode>
    {
        // Most yaml mappings are a handful of fields so we just keep them as an array and not a dictionary.
        private const int SmallMappingCapacity = 8;

        private object _storage;

        /// <summary>
        /// ValueDataNodes associated with each key. This is used for yaml validation / error reporting.
        /// I.e., if a key is meant to be an EntityPrototype ID, we want to print an error that points to the
        /// corresponding yaml lines.
        /// </summary>
        private IReadOnlyDictionary<string, ValueDataNode>? _keyNodes;
        // TODO avoid populating this unless we are running the yaml linter?

        private bool IsSmall => _storage is KeyValuePair<string, DataNode>[];
        private KeyValuePair<string, DataNode>[] SmallEntries => (KeyValuePair<string, DataNode>[]) _storage;
        private Dictionary<string, DataNode> Dictionary => (Dictionary<string, DataNode>) _storage;

        private int SmallCount
        {
            get
            {
                var entries = SmallEntries;
                var count = 0;
                while (count < entries.Length && entries[count].Key != null)
                {
                    count++;
                }

                return count;
            }
        }

        public override bool IsEmpty => Count == 0;
        public int Count => IsSmall ? SmallCount : Dictionary.Count;
        public bool IsReadOnly => false;
        public IReadOnlyDictionary<string, DataNode> Children => this;

        public MappingDataNode() : this(0)
        {
        }

        public MappingDataNode(int size) : base(NodeMark.Invalid, NodeMark.Invalid)
        {
            _storage = size <= SmallMappingCapacity
                ? size == 0
                    ? Array.Empty<KeyValuePair<string, DataNode>>()
                    : new KeyValuePair<string, DataNode>[size]
                : new Dictionary<string, DataNode>(size);
        }

        public MappingDataNode(YamlMappingNode mapping) : this(mapping.Children.Count)
        {
            Start = mapping.Start;
            End = mapping.End;

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

        public MappingDataNode(Dictionary<string, DataNode> nodes) : this(nodes.Count)
        {
            foreach (var (key, value) in nodes)
            {
                Add(key, value);
            }
        }

        public KeyValuePair<string, DataNode> this[int key]
        {
            get
            {
                if (key < 0 || key >= Count)
                    throw new ArgumentOutOfRangeException(nameof(key));

                if (IsSmall)
                    return SmallEntries[key];

                var index = 0;
                foreach (var entry in Dictionary)
                {
                    if (index++ == key)
                        return entry;
                }

                throw new InvalidOperationException("Mapping changed while being indexed");
            }
        }

        public MappingDataNode Add(string key, DataNode node)
        {
            if (!TryAdd(key, node))
                throw new ArgumentException($"An item with the same key has already been added. Key: {key}", nameof(key));

            return this;
        }

        public DataNode this[string key]
        {
            get => Get(key);
            set
            {
                if (IsSmall)
                {
                    var entries = SmallEntries;
                    var count = SmallCount;
                    for (var i = 0; i < count; i++)
                    {
                        if (entries[i].Key != key)
                            continue;

                        entries[i] = new KeyValuePair<string, DataNode>(key, value);
                        return;
                    }

                    Add(key, value);
                    return;
                }

                Dictionary[key] = value;
            }
        }

        public int IndexOf(string key)
        {
            if (IsSmall)
            {
                var entries = SmallEntries;
                var count = SmallCount;
                for (var index = 0; index < count; index++)
                {
                    if (entries[index].Key == key)
                        return index;
                }

                return -1;
            }

            var result = 0;
            foreach (var entry in Dictionary)
            {
                if (entry.Key == key)
                    return result;

                result++;
            }

            return -1;
        }

        void IDictionary<string, DataNode>.Add(string key, DataNode value) => Add(key, value);

        public bool ContainsKey(string key)
        {
            if (!IsSmall)
                return Dictionary.ContainsKey(key);

            return IndexOf(key) != -1;
        }

        bool IDictionary<string, DataNode>.Remove(string key) => Remove(key);

        public bool TryGetValue(string key, [NotNullWhen(true)] out DataNode? value)
            => TryGet(key, out value);

        // TODO consider changing these to unsorted collections.
        // Keeping the public ICollection API retains IDictionary compatibility without retaining a parallel List.
        public ICollection<string> Keys
        {
            get
            {
                var keys = new string[Count];
                var index = 0;
                foreach (var (key, _) in this)
                {
                    keys[index++] = key;
                }

                return keys;
            }
        }

        public ICollection<DataNode> Values
        {
            get
            {
                var values = new DataNode[Count];
                var index = 0;
                foreach (var (_, value) in this)
                {
                    values[index++] = value;
                }

                return values;
            }
        }

        IEnumerable<string> IReadOnlyDictionary<string, DataNode>.Keys => EnumerateKeys();
        IEnumerable<DataNode> IReadOnlyDictionary<string, DataNode>.Values => EnumerateValues();

        public DataNode Get(string key)
        {
            if (TryGet(key, out var node))
                return node;

            throw new KeyNotFoundException();
        }

        public T Get<T>(string key) where T : DataNode
        {
            return (T) Get(key);
        }

        public bool TryGet(string key, [NotNullWhen(true)] out DataNode? node)
        {
            if (!IsSmall)
                return Dictionary.TryGetValue(key, out node);

            var entries = SmallEntries;
            var count = SmallCount;
            for (var i = 0; i < count; i++)
            {
                if (entries[i].Key == key)
                {
                    node = entries[i].Value;
                    return true;
                }
            }

            node = null;
            return false;
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
            return ContainsKey(key);
        }

        public bool Remove(string key)
        {
            if (!IsSmall)
                return Dictionary.Remove(key);

            var entries = SmallEntries;
            var count = SmallCount;
            for (var index = 0; index < count; index++)
            {
                if (entries[index].Key != key)
                    continue;

                Array.Copy(entries, index + 1, entries, index, count - index - 1);
                entries[count - 1] = default;
                return true;
            }

            return false;
        }

        public T Cast<T>(string key) where T : DataNode
        {
            return (T) this[key];
        }

        public YamlMappingNode ToYaml()
        {
            var mapping = new YamlMappingNode();

            foreach (var (key, val) in this)
            {
                YamlScalarNode yamlKeyNode;
                if (_keyNodes != null && _keyNodes.TryGetValue(key, out var keyNode))
                {
                    yamlKeyNode = (YamlScalarNode)keyNode;
                }
                else
                {
                    // This matches the ValueDataNode -> YamlScalarNode cast operator.
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

            // TODO Serialization: should prob make this smarter.
            newMapping.Tag = Tag;
            newMapping.Start = Start;
            newMapping.End = End;

            return newMapping;
        }

        public void Insert(MappingDataNode otherMapping, bool skipDuplicates = false)
        {
            foreach (var (key, val) in otherMapping)
            {
                if (!skipDuplicates || !Has(key))
                {
                    // Intentionally raises an ArgumentException.
                    Add(key, val.Copy());
                }
            }
        }

        public void InsertAt(int index, string key, DataNode value)
        {
            if (index > Count || index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (ContainsKey(key))
                throw new InvalidOperationException($"Already contains key {key}");

            if (IsSmall && Count < SmallMappingCapacity)
            {
                var entries = SmallEntries;
                var count = SmallCount;
                if (count == entries.Length)
                {
                    // Double it and pass it onto the next person.
                    var newEntries = new KeyValuePair<string, DataNode>[Math.Min(SmallMappingCapacity, Math.Max(1, count * 2))];
                    Array.Copy(entries, newEntries, count);
                    entries = newEntries;
                    _storage = entries;
                }

                Array.Copy(entries, index, entries, index + 1, count - index);
                entries[index] = new KeyValuePair<string, DataNode>(key, value);
                return;
            }

            // IDictionary has no ordering guarantee. Rebuilding is the rare case and keeps the existing indexer/ToYaml behaviour without retaining a List for every mapping.
            var replacement = new Dictionary<string, DataNode>(Count + 1);
            var currentIndex = 0;
            foreach (var entry in this)
            {
                if (currentIndex++ == index)
                    replacement.Add(key, value);

                replacement.Add(entry.Key, entry.Value);
            }

            if (currentIndex == index)
                replacement.Add(key, value);

            _storage = replacement;
        }

        public override MappingDataNode Copy()
        {
            var newMapping = new MappingDataNode(Count)
            {
                Tag = Tag,
                Start = Start,
                End = End
            };

            foreach (var (key, val) in this)
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
            var newMapping = new MappingDataNode(Count)
            {
                Tag = Tag,
                Start = Start,
                End = End
            };

            foreach (var (key, val) in this)
            {
                newMapping.Add(key, val);
            }

            newMapping._keyNodes = _keyNodes;
            return newMapping;
        }

        /// <summary>
        /// Variant of <see cref="Except(MappingDataNode)"/> that will recursively call except rather than only checking equality.
        /// </summary>
        public MappingDataNode? RecursiveExcept(MappingDataNode node)
        {
            var mappingNode = new MappingDataNode()
            {
                Tag = Tag,
                Start = Start,
                End = End
            };

            foreach (var (key, val) in this)
            {
                if (!node.TryGet(key, out var otherVal))
                {
                    mappingNode.Add(key, val.Copy());
                }
                else if (val.Except(otherVal) is { } newValue)
                {
                    mappingNode.Add(key, newValue);
                }
            }

            return mappingNode.Count == 0 ? null : mappingNode;
        }

        public override MappingDataNode? Except(MappingDataNode node)
        {
            var mappingNode = new MappingDataNode()
            {
                Tag = Tag,
                Start = Start,
                End = End
            };

            foreach (var (key, val) in this)
            {
                if (!node.TryGet(key, out var otherVal) || !val.Equals(otherVal))
                    mappingNode.Add(key, val.Copy());
            }

            return mappingNode.Count == 0 ? null : mappingNode;
        }

        /// <summary>
        /// Returns true if there are any nodes on this node that aren't in the other node.
        /// </summary>
        [Pure]
        public bool AnyExcept(MappingDataNode node)
        {
            foreach (var (key, val) in this)
            {
                if (!node.TryGet(key, out var otherValue) || !val.Equals(otherValue))
                    return true;
            }

            return false;
        }

        public override bool Equals(object? obj)
        {
            if (obj is not MappingDataNode other)
                return false;

            if (Count != other.Count)
                return false;

            if (Tag != other.Tag)
                return false;

            foreach (var (key, otherValue) in other)
            {
                if (!TryGet(key, out var ownValue) || !otherValue.Equals(ownValue))
                    return false;
            }

            return true;
        }

        public Enumerator GetEnumerator() => new(this);
        IEnumerator<KeyValuePair<string, DataNode>> IEnumerable<KeyValuePair<string, DataNode>>.GetEnumerator() => GetEnumerator();

        public override int GetHashCode()
        {
            var code = new HashCode();
            foreach (var (key, value) in this)
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
            if (IsSmall)
                Array.Clear(SmallEntries);
            else
                Dictionary.Clear();
        }

        public bool Contains(KeyValuePair<string, DataNode> item) => ContainsKey(item.Key);

        [Obsolete("Use SerializationManager.PushComposition()")]
        public override MappingDataNode PushInheritance(MappingDataNode node)
        {
            var newNode = Copy();
            foreach (var (key, val) in node)
            {
                if (ContainsKey(key))
                    continue;

                newNode.Remove(key);
                newNode.Add(key, val.Copy());
            }

            return newNode;
        }

        public void CopyTo(KeyValuePair<string, DataNode>[] array, int arrayIndex)
        {
            foreach (var entry in this)
            {
                array[arrayIndex++] = entry;
            }
        }

        public bool Remove(KeyValuePair<string, DataNode> item) => Remove(item.Key);

        public bool TryAdd(string key, DataNode value)
        {
            if (!IsSmall)
                return Dictionary.TryAdd(key, value);

            var entries = SmallEntries;
            var count = SmallCount;
            for (var i = 0; i < count; i++)
            {
                if (entries[i].Key == key)
                    return false;
            }

            if (count < entries.Length)
            {
                entries[count] = new KeyValuePair<string, DataNode>(key, value);
                return true;
            }

            if (count < SmallMappingCapacity)
            {
                var newEntries = new KeyValuePair<string, DataNode>[Math.Min(SmallMappingCapacity, Math.Max(1, count * 2))];
                Array.Copy(entries, newEntries, count);
                newEntries[count] = new KeyValuePair<string, DataNode>(key, value);
                _storage = newEntries;
                return true;
            }

            PromoteToDictionary();
            return Dictionary.TryAdd(key, value);
        }

        public bool TryAddCopy(string key, DataNode value)
        {
            if (IsSmall)
            {
                if (ContainsKey(key))
                    return false;

                Add(key, value.Copy());
                return true;
            }

            ref var entry = ref CollectionsMarshal.GetValueRefOrAddDefault(Dictionary, key, out var exists);
            if (exists)
                return false;

            entry = value.Copy();
            return true;
        }

        private void PromoteToDictionary()
        {
            var entries = SmallEntries;
            var count = SmallCount;
            var dictionary = new Dictionary<string, DataNode>(SmallMappingCapacity + 1);
            for (var i = 0; i < count; i++)
            {
                dictionary.Add(entries[i].Key, entries[i].Value);
            }

            _storage = dictionary;
        }

        private IEnumerable<string> EnumerateKeys()
        {
            foreach (var (key, _) in this)
            {
                yield return key;
            }
        }

        private IEnumerable<DataNode> EnumerateValues()
        {
            foreach (var (_, value) in this)
            {
                yield return value;
            }
        }

        public struct Enumerator : IEnumerator<KeyValuePair<string, DataNode>>
        {
            private readonly KeyValuePair<string, DataNode>[]? _smallEntries;
            private Dictionary<string, DataNode>.Enumerator _dictionaryEnumerator;
            private int _index;

            internal Enumerator(MappingDataNode mapping)
            {
                if (mapping.IsSmall)
                {
                    _smallEntries = mapping.SmallEntries;
                    _dictionaryEnumerator = default;
                }
                else
                {
                    _smallEntries = null;
                    _dictionaryEnumerator = mapping.Dictionary.GetEnumerator();
                }

                _index = -1;
            }

            public KeyValuePair<string, DataNode> Current => _smallEntries == null
                ? _dictionaryEnumerator.Current
                : _smallEntries[_index];

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (_smallEntries == null)
                    return _dictionaryEnumerator.MoveNext();

                _index++;
                return _index < _smallEntries.Length && _smallEntries[_index].Key != null;
            }

            public void Reset()
            {
                throw new NotSupportedException();
            }

            public void Dispose()
            {
                _dictionaryEnumerator.Dispose();
            }
        }

        // These methods are probably fine to keep around as helper methods, but are currently marked as obsolete
        // so that people don't unnecessarily allocate a ValueDataNode. I.e., to prevent people from using code like
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
        public bool Contains(KeyValuePair<ValueDataNode, DataNode> item) => ContainsKey(item.Key.Value);

        [Obsolete("Use string keys instead of ValueDataNode")]
        public bool Remove(ValueDataNode key) => Remove(key.Value);

        #endregion
    }
}
