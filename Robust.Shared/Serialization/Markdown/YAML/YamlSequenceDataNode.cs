using System.Collections.Generic;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Serialization.Markdown.YAML
{
    public class YamlSequenceDataNode : ISequenceDataNode
    {
        private List<IDataNode> nodes = new();

        public YamlSequenceDataNode(YamlSequenceNode sequenceNode)
        {
            foreach (var node in sequenceNode.Children)
            {
                nodes.Add(node.ToDataNode());
            }
        }

        public IReadOnlyList<IDataNode> Sequence => nodes;
    }
}
