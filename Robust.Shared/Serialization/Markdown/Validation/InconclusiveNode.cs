using System.Collections.Generic;

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
        public override IEnumerable<string> Invalids()
        {
            yield break;
        }

        public override string? ToString()
        {
            return DataNode.ToString();
        }
    }
}
