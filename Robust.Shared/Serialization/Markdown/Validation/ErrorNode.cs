using System;
using System.Collections.Generic;

namespace Robust.Shared.Serialization.Markdown.Validation
{
    public class ErrorNode : ValidationNode, IEquatable<ErrorNode>
    {
        public ErrorNode(DataNode node, string errorReason, bool alwaysRelevant = true)
        {
            Node = node;
            ErrorReason = errorReason;
            AlwaysRelevant = alwaysRelevant;
        }

        public DataNode Node { get; }

        public string ErrorReason { get; }

        public bool AlwaysRelevant { get; }

        public override bool Valid => false;

        public override IEnumerable<ErrorNode> GetErrors()
        {
            yield return this;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Node, ErrorReason, AlwaysRelevant);
        }

        public bool Equals(ErrorNode? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return Node.Equals(other.Node) &&
                   ErrorReason == other.ErrorReason &&
                   AlwaysRelevant == other.AlwaysRelevant;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;

            return Equals((ErrorNode) obj);
        }

        public static bool operator ==(ErrorNode? left, ErrorNode? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ErrorNode? left, ErrorNode? right)
        {
            return !Equals(left, right);
        }
    }
}
