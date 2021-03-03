using System.Collections.Generic;

namespace Robust.Shared.Serialization.Markdown.Validation
{
    public class ErrorNode : ValidatedNode
    {
        public readonly DataNode Node;
        public string ErrorReason;

        public ErrorNode(DataNode node, string errorReason)
        {
            Node = node;
            ErrorReason = errorReason;
        }

        public override bool Valid => false;

        public override IEnumerable<string> Invalids()
        {
            var str = Node.ToString();
            if (str != null)
            {
                yield return $"{str} ({ErrorReason})";
            }
        }
    }
}
