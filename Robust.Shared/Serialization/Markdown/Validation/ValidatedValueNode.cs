using System.Collections.Generic;
using Fluent.Net.RuntimeAst;

namespace Robust.Shared.Serialization.Markdown.Validation
{
    public class ValidatedValueNode : ValidatedNode
    {
        public readonly DataNode DataNode;
        public override bool Valid => true;
        public override IEnumerable<string> Invalids()
        {
            yield break;
        }

        public ValidatedValueNode(DataNode dataNode)
        {
            DataNode = dataNode;
        }

        public override string? ToString()
        {
            return DataNode.ToString();
        }
    }
}
