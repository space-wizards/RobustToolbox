using System.Collections.Generic;
using System.Linq;

namespace Robust.Shared.Serialization.Markdown.Validation
{
    public class InconclusiveNode : ValidationNode
    {
        public readonly DataNode DataNode;

        public InconclusiveNode(DataNode dataNode)
        {
            DataNode = dataNode;
        }

        public override bool Valid => true;

        public override IEnumerable<ErrorNode> GetErrors() => Enumerable.Empty<ErrorNode>();

        public override string? ToString()
        {
            return DataNode.ToString();
        }
    }
}
