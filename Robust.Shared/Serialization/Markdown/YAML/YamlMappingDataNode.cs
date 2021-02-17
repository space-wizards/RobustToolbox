using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Serialization.Markdown.YAML
{
    public class YamlMappingDataNode : IMappingDataNode
    {
        private Dictionary<IDataNode, IDataNode> _mapping = new();

        public IReadOnlyDictionary<IDataNode, IDataNode> Children => _mapping;

        public YamlMappingDataNode() { }

        public YamlMappingDataNode(YamlMappingNode mapping)
        {
            foreach (var (key, val) in mapping.Children)
            {
                _mapping.Add(key.ToDataNode(), val.ToDataNode());
            }
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

        public IDataNode GetNode(IDataNode key)
        {
            return _mapping[key];
        }

        public IDataNode GetNode(string key)
        {
            return GetNode(_getFetchNode(key));
        }

        public bool TryGetNode(IDataNode key, [NotNullWhen(true)] out IDataNode? node)
        {
            if (_mapping.TryGetValue(key, out node))
            {
                return true;
            }

            node = null;
            return false;
        }

        public bool TryGetNode(string key, [NotNullWhen(true)] out IDataNode? node)
        {
            return TryGetNode(_getFetchNode(key), out node);
        }

        public bool HasNode(IDataNode key)
        {
            return _mapping.ContainsKey(key);
        }

        public bool HasNode(string key)
        {
            return HasNode(_getFetchNode(key));
        }

        public void AddNode(IDataNode key, IDataNode node)
        {
            _mapping.Add(key, node);
        }

        public void AddNode(string key, IDataNode node)
        {
            AddNode(_getFetchNode(key), node);
        }

        public void RemoveNode(IDataNode key)
        {
            _mapping.Remove(key);
        }

        public void RemoveNode(string key)
        {
            RemoveNode(_getFetchNode(key));
        }

        public IMappingDataNode Merge(IMappingDataNode otherMapping)
        {
            var newMapping = (Copy() as IMappingDataNode)!;
            foreach (var (key, val) in otherMapping.Children)
            {
                //intentionally provokes argumentexception
                newMapping.AddNode(key.Copy(), val.Copy());
            }

            return newMapping;
        }

        public IDataNode Copy()
        {
            var newMapping = new YamlMappingDataNode();
            foreach (var (key, val) in _mapping)
            {
                newMapping.AddNode(key.Copy(), val.Copy());
            }

            return newMapping;
        }

        // To fetch nodes by key name with YAML, we NEED a YamlScalarNode.
        // We use a thread local one to avoid allocating one every fetch, since we just replace the inner value.
        // Obviously thread local to avoid threading issues.
        private static readonly ThreadLocal<YamlValueDataNode> FetchNode =
            new(() => new YamlValueDataNode(""));

        private static YamlValueDataNode _getFetchNode(string key)
        {
            var node = FetchNode.Value!;
            node.Value = key;
            return node;
        }
    }
}
