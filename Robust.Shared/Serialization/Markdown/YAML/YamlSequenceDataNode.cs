using System.Collections.Generic;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Serialization.Markdown.YAML
{
    public class YamlSequenceDataNode : ISequenceDataNode
    {
        private List<IDataNode> nodes = new();

        public YamlSequenceDataNode() { }

        public YamlSequenceDataNode(YamlSequenceNode sequenceNode)
        {
            foreach (var node in sequenceNode.Children)
            {
                nodes.Add(node.ToDataNode());
            }
        }

        public YamlSequenceNode ToSequenceNode()
        {
            var node = new YamlSequenceNode();
            foreach (var dataNode in nodes)
            {
                node.Children.Add(dataNode.ToYamlNode());
            }

            return node;
        }

        public IReadOnlyList<IDataNode> Sequence => nodes;
        public void Add(IDataNode node)
        {
            nodes.Add(node);
        }

        public IDataNode Copy()
        {
            var newSequence = new YamlSequenceDataNode();
            foreach (var node in Sequence)
            {
                newSequence.Add(node.Copy());
            }

            return newSequence;
        }
    }
}
