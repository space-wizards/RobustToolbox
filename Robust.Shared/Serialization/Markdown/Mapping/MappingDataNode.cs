using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Serialization.Markdown.Mapping
{
    public class MappingDataNode : DataNode<MappingDataNode>
    {
        // To fetch nodes by key name with YAML, we NEED a YamlScalarNode.
        // We use a thread local one to avoid allocating one every fetch, since we just replace the inner value.
        // Obviously thread local to avoid threading issues.
        private static readonly ThreadLocal<ValueDataNode> FetchNode =
            new(() => new ValueDataNode(""));

        private readonly Dictionary<DataNode, DataNode> _children = new();

        public IReadOnlyDictionary<DataNode, DataNode> Children => _children;

        public MappingDataNode() : base(NodeMark.Invalid, NodeMark.Invalid)
        {
        }

        public MappingDataNode(YamlMappingNode mapping) : base(mapping.Start, mapping.End)
        {
            foreach (var (key, val) in mapping.Children)
            {
                _children.Add(key.ToDataNode(), val.ToDataNode());
            }

            Tag = mapping.Tag;
        }

        public MappingDataNode(Dictionary<DataNode, DataNode> nodes) : this()
        {
            foreach (var (key, val) in nodes)
            {
                _children.Add(key, val);
            }
        }

        public KeyValuePair<DataNode, DataNode> this[int key] => Children.ElementAt(key);

        public DataNode this[string index]
        {
            get => Get(index);
            set => Add(new ValueDataNode(index), value);
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
            return this;
        }

        public DataNode Get(DataNode key)
        {
            return _children[key];
        }

        public DataNode Get(string key)
        {
            return Get(GetFetchNode(key));
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

        public bool TryGet(string key, [NotNullWhen(true)] out DataNode? node)
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
            _children.Remove(key);
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
            foreach (var (key, val) in _children)
            {
                mapping.Add(key.ToYamlNode(), val.ToYamlNode());
            }

            mapping.Tag = Tag;

            return mapping;
        }

        public MappingDataNode Merge(MappingDataNode otherMapping)
        {
            var newMapping = Copy();
            foreach (var (key, val) in otherMapping.Children)
            {
                // Intentionally raises an ArgumentException
                newMapping.Add(key.Copy(), val.Copy());
            }

            // TODO Serialization: should prob make this smarter
            newMapping.Tag = Tag;
            newMapping.Start = Start;
            newMapping.End = End;

            return newMapping;
        }

        public override MappingDataNode Copy()
        {
            var newMapping = new MappingDataNode()
            {
                Tag = Tag,
                Start = Start,
                End = End
            };

            foreach (var (key, val) in _children)
            {
                newMapping.Add(key.Copy(), val.Copy());
            }

            return newMapping;
        }

        public override MappingDataNode? Except(MappingDataNode node)
        {
            var mappingNode = new MappingDataNode()
            {
                Tag = Tag,
                Start = Start,
                End = End
            };

            foreach (var (key, val) in _children)
            {
                var other = node._children.FirstOrNull(p => p.Key.Equals(key));
                if (other == null)
                {
                    mappingNode.Add(key.Copy(), val.Copy());
                }
                else
                {
                    var newValue = val.Except(other.Value.Value);
                    if (newValue == null) continue;
                    mappingNode.Add(key.Copy(), newValue);
                }
            }

            if (mappingNode._children.Count == 0) return null;

            return mappingNode;
        }

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
    }
}
