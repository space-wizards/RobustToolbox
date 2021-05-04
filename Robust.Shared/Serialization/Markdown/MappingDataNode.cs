using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Serialization.Markdown
{
    public class MappingDataNode : DataNode<MappingDataNode>
    {
        private Dictionary<DataNode, DataNode> _mapping = new();
        public IReadOnlyDictionary<DataNode, DataNode> Children => _mapping;

        public MappingDataNode() : base(NodeMark.Invalid, NodeMark.Invalid)
        { }

        public MappingDataNode(YamlMappingNode mapping) : base(mapping.Start, mapping.End)
        {
            foreach (var (key, val) in mapping.Children)
            {
                _mapping.Add(key.ToDataNode(), val.ToDataNode());
            }

            Tag = mapping.Tag;
        }

        public MappingDataNode(Dictionary<DataNode, DataNode> nodes) : this()
        {
            foreach (var (key, val) in nodes)
            {
                _mapping.Add(key, val);
            }
        }

        public KeyValuePair<DataNode, DataNode> this[int key] => Children.ElementAt(key);

        public DataNode this[string index]
        {
            get => GetNode(index);
            set => AddNode(index, value);
        }

        public YamlMappingNode ToMappingNode()
        {
            var mapping = new YamlMappingNode();
            foreach (var (key, val) in _mapping)
            {
                mapping.Add(key.ToYamlNode(), val.ToYamlNode());
            }

            mapping.Tag = Tag;

            return mapping;
        }

        public T Cast<T>(string index) where T : DataNode
        {
            return (T) this[index];
        }

        public DataNode GetNode(DataNode key)
        {
            return _mapping[key];
        }

        public DataNode GetNode(string key)
        {
            return GetNode(_getFetchNode(key));
        }

        public bool TryGetNode(DataNode key, [NotNullWhen(true)] out DataNode? node)
        {
            if (_mapping.TryGetValue(key, out node))
            {
                return true;
            }

            node = null;
            return false;
        }

        public bool TryGetNode(string key, [NotNullWhen(true)] out DataNode? node)
        {
            return TryGetNode(_getFetchNode(key), out node);
        }

        public bool HasNode(DataNode key)
        {
            return _mapping.ContainsKey(key);
        }

        public bool HasNode(string key)
        {
            return HasNode(_getFetchNode(key));
        }

        public void AddNode(DataNode key, DataNode node)
        {
            _mapping.Add(key, node);
        }

        public void AddNode(string key, DataNode node)
        {
            AddNode(new ValueDataNode(key), node);
        }

        public MappingDataNode RemoveNode(DataNode key)
        {
            _mapping.Remove(key);
            return this;
        }

        public MappingDataNode RemoveNode(string key)
        {
            return RemoveNode(_getFetchNode(key));
        }

        public MappingDataNode Merge(MappingDataNode otherMapping)
        {
            var newMapping = Copy();
            foreach (var (key, val) in otherMapping.Children)
            {
                // Intentionally raises an ArgumentException
                newMapping.AddNode(key.Copy(), val.Copy());
            }

            newMapping.Tag = Tag;

            // TODO Serialization: should prob make this smarter
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

            foreach (var (key, val) in _mapping)
            {
                newMapping.AddNode(key.Copy(), val.Copy());
            }

            return newMapping;
        }

        public override MappingDataNode? Except(MappingDataNode node)
        {
            var mappingNode = new MappingDataNode(){Tag = Tag, Start = Start, End = End};

            foreach (var (key, val) in _mapping)
            {
                var other = node._mapping.FirstOrNull(p => p.Key.Equals(key));
                if (other == null)
                {
                    mappingNode.AddNode(key.Copy(), val.Copy());
                }
                else
                {
                    var newValue = val.Except(other.Value.Value);
                    if(newValue == null) continue;
                    mappingNode.AddNode(key.Copy(), newValue);
                }
            }

            if (mappingNode._mapping.Count == 0) return null;

            return mappingNode;
        }

        public override int GetHashCode()
        {
            var code = new HashCode();
            foreach (var (key, value) in _mapping)
            {
                code.Add(key);
                code.Add(value);
            }

            return code.ToHashCode();
        }

        // To fetch nodes by key name with YAML, we NEED a YamlScalarNode.
        // We use a thread local one to avoid allocating one every fetch, since we just replace the inner value.
        // Obviously thread local to avoid threading issues.
        private static readonly ThreadLocal<ValueDataNode> FetchNode =
            new(() => new ValueDataNode(""));

        private static ValueDataNode _getFetchNode(string key)
        {
            var node = FetchNode.Value!;
            node.Value = key;
            return node;
        }
    }
}
