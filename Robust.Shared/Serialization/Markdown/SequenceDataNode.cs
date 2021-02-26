using System.Collections.Generic;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Serialization.Markdown
{
    public class SequenceDataNode : DataNode
    {
        private readonly List<DataNode> _nodes = new();

        public SequenceDataNode() { }

        public SequenceDataNode(YamlSequenceNode sequenceNode)
        {
            foreach (var node in sequenceNode.Children)
            {
                _nodes.Add(node.ToDataNode());
            }

            Tag = sequenceNode.Tag;
        }

        public SequenceDataNode(params DataNode[] nodes)
        {
            foreach (var node in nodes)
            {
                _nodes.Add(node);
            }
        }

        public YamlSequenceNode ToSequenceNode()
        {
            var node = new YamlSequenceNode();
            foreach (var dataNode in _nodes)
            {
                node.Children.Add(dataNode.ToYamlNode());
            }

            return node;
        }

        public IReadOnlyList<DataNode> Sequence => _nodes;

        public DataNode this[int index] => _nodes[index];

        public void Add(DataNode node)
        {
            _nodes.Add(node);
        }

        public void Remove(DataNode node)
        {
            _nodes.Remove(node);
        }

        public T Cast<T>(int index) where T : DataNode
        {
            return (T) this[index];
        }

        public override DataNode Copy()
        {
            var newSequence = new SequenceDataNode() {Tag = Tag};

            foreach (var node in Sequence)
            {
                newSequence.Add(node.Copy());
            }

            return newSequence;
        }
    }
}
