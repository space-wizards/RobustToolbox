using System.Collections.Generic;
using System.Linq;

namespace Robust.Shared.Serialization.Markdown.Validation
{
    public class ValidatedValueNode : ValidatedNode
    {
        public readonly DataNode DataNode;

        public ValidatedValueNode(DataNode dataNode)
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
