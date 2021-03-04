using System;
using System.Collections.Generic;

namespace Robust.Shared.Serialization.Markdown.Validation
{
    public class ErrorNode : ValidationNode
    {
        public readonly DataNode Node;
        public readonly string ErrorReason;
        public readonly bool AlwaysRelevant;

        public ErrorNode(DataNode node, string errorReason, bool alwaysRelevant = false)
        {
            Node = node;
            ErrorReason = errorReason;
            AlwaysRelevant = alwaysRelevant;
        }

        public override bool Valid => false;

        public override IEnumerable<ErrorNode> GetErrors()
        {
            yield return this;
        }

        public override int GetHashCode()
        {
            var code = new HashCode();
            code.Add(Node.Start.GetHashCode());
            code.Add(Node.End.GetHashCode());
            code.Add(ErrorReason.GetHashCode());
            return code.ToHashCode();
        }

        public override bool Equals(object? obj)
        {
            if (obj is not ErrorNode node) return false;
            return Node.GetHashCode() == node.Node.GetHashCode(); // ErrorReason == node.ErrorReason
        }
    }
}
