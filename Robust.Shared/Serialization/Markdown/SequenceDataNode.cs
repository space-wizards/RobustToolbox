using System;
using System.Collections.Generic;
using Robust.Shared.Serialization.Markdown.YAML;
using YamlDotNet.Core.Tokens;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Serialization.Markdown
{
    public class SequenceDataNode : DataNode
    {
        private List<DataNode> nodes = new();

        public SequenceDataNode() { }

        public SequenceDataNode(YamlSequenceNode sequenceNode)
        {
            foreach (var node in sequenceNode.Children)
            {
                nodes.Add(node.ToDataNode());
            }

            Tag = sequenceNode.Tag;
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

        public IReadOnlyList<DataNode> Sequence => nodes;
        public void Add(DataNode node)
        {
            nodes.Add(node);
        }

        public void Remove(DataNode node)
        {
            nodes.Remove(node);
        }

        public override DataNode Copy()
        {
            var newSequence = new SequenceDataNode();
            foreach (var node in Sequence)
            {
                newSequence.Add(node.Copy());
            }

            return newSequence;
        }
    }
}
