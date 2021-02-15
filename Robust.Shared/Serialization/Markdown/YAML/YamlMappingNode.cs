using System.Diagnostics.CodeAnalysis;
using System.Threading;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Serialization.Markdown.YAML
{
    public class YamlMappingDataNode : IMappingDataNode
    {
        private YamlMappingNode mapping;

        public YamlMappingDataNode(YamlMappingNode mapping)
        {
            this.mapping = mapping;
        }

        public bool TryGetNode(string key, [NotNullWhen(true)] out IDataNode? node)
        {
            if (mapping.Children.TryGetValue(_getFetchNode(key), out var yamlNode))
            {
                node = yamlNode.ToDataNode();
                return true;
            }

            node = null;
            return false;

        }

        public bool HasNode(string key)
        {
            return mapping.Children.ContainsKey(_getFetchNode(key));
        }

        // To fetch nodes by key name with YAML, we NEED a YamlScalarNode.
        // We use a thread local one to avoid allocating one every fetch, since we just replace the inner value.
        // Obviously thread local to avoid threading issues.
        private static readonly ThreadLocal<YamlScalarNode> FetchNode =
            new(() => new YamlScalarNode());

        private static YamlScalarNode _getFetchNode(string key)
        {
            var node = FetchNode.Value!;
            node.Value = key;
            return node;
        }
    }
}
