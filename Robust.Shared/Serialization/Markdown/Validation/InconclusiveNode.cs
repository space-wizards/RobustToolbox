using System.Collections.Generic;
using System.Linq;

namespace Robust.Shared.Serialization.Markdown.Validation
{
    public class InconclusiveNode : ValidatedNode
    {
        public readonly DataNode DataNode;

        public InconclusiveNode(DataNode dataNode)
        {
            DataNode = dataNode;
        }

        public override bool Valid => true;

        public override IEnumerable<string> Invalids() => Enumerable.Empty<string>();

        public override string? ToString()
        {
            return DataNode.ToString();
        }
    }
}
