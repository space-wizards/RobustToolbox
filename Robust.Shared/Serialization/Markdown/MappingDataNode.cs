using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Robust.Shared.Serialization.Markdown.YAML;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Serialization.Markdown
{
    public class MappingDataNode : DataNode
    {
        private Dictionary<DataNode, DataNode> _mapping = new();
        public IReadOnlyDictionary<DataNode, DataNode> Children => _mapping;

        public MappingDataNode() { }

        public KeyValuePair<DataNode, DataNode> this[int key] => Children.ElementAt(key);

        public DataNode this[string index]
        {
            get => GetNode(index);
            set => AddNode(index, value);
        }

        public MappingDataNode(YamlMappingNode mapping)
        {
            foreach (var (key, val) in mapping.Children)
            {
                _mapping.Add(key.ToDataNode(), val.ToDataNode());
            }

            Tag = mapping.Tag;
        }

        public YamlMappingNode ToMappingNode()
        {
            var mapping = new YamlMappingNode();
            foreach (var (key, val) in _mapping)
            {
                mapping.Add(key.ToYamlNode(), val.ToYamlNode());
            }

            return mapping;
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
            //todo paul yes yes i'll rework it
            AddNode(new ValueDataNode(key), node);
        }

        public void RemoveNode(DataNode key)
        {
            _mapping.Remove(key);
        }

        public void RemoveNode(string key)
        {
            RemoveNode(_getFetchNode(key));
        }

        public MappingDataNode Merge(MappingDataNode otherMapping)
        {
            var newMapping = (Copy() as MappingDataNode)!;
            foreach (var (key, val) in otherMapping.Children)
            {
                //intentionally provokes argumentexception
                newMapping.AddNode(key.Copy(), val.Copy());
            }

            newMapping.Tag = Tag;

            return newMapping;
        }

        public override DataNode Copy()
        {
            var newMapping = new MappingDataNode() {Tag = Tag};

            foreach (var (key, val) in _mapping)
            {
                newMapping.AddNode(key.Copy(), val.Copy());
            }

            return newMapping;
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
