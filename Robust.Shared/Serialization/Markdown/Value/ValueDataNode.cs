using System;
using System.Globalization;
using JetBrains.Annotations;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Serialization.Markdown.Value
{
    public sealed class ValueDataNode : DataNode<ValueDataNode>, IEquatable<ValueDataNode>
    {
        public static ValueDataNode Null() => new((string?)null);

        public ValueDataNode() : this(string.Empty) {}

        public ValueDataNode(string? value) : base(NodeMark.Invalid, NodeMark.Invalid)
        {
            Value = value ?? string.Empty;
            IsNull = value == null;
        }

        public ValueDataNode(YamlScalarNode node) : base(node.Start, node.End)
        {
            IsNull = node.Style != ScalarStyle.DoubleQuoted && node.Style != ScalarStyle.SingleQuoted &&
                     (node.Value == null || IsNullLiteral(node.Value));
            Value = node.Value ?? string.Empty;
            Tag = node.Tag.IsEmpty ? null : node.Tag.Value;
        }

        public static explicit operator YamlScalarNode(ValueDataNode node)
        {
            if (node.IsNull)
            {
                return new YamlScalarNode("null"){Tag = node.Tag};
            }

            return new YamlScalarNode(node.Value)
            {
                Tag = node.Tag,
                Style = IsNullLiteral(node.Value) ? ScalarStyle.DoubleQuoted : ScalarStyle.Any
            };
        }

        public string Value { get; set; }
        public override bool IsNull { get; init; }

        public override bool IsEmpty => string.IsNullOrWhiteSpace(Value);

        private static bool IsNullLiteral(string value) => value.Trim().ToLower() is "null" || string.IsNullOrWhiteSpace(value);

        public override ValueDataNode Copy()
        {
            return new(Value)
            {
                Tag = Tag,
                Start = Start,
                End = End,
                IsNull = IsNull
            };
        }

        public override ValueDataNode? Except(ValueDataNode node)
        {
            return node.Value == Value ? null : Copy();
        }

        public override ValueDataNode PushInheritance(ValueDataNode node)
        {
            return Copy();
        }

        public override bool Equals(object? obj)
        {
            return obj is ValueDataNode node && Equals(node);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString()
        {
            return Value;
        }

        [Pure]
        public int AsInt()
        {
            return int.Parse(Value, CultureInfo.InvariantCulture);
        }

        [Pure]
        public float AsFloat()
        {
            return float.Parse(Value, CultureInfo.InvariantCulture);
        }

        [Pure]
        public bool AsBool()
        {
            return bool.Parse(Value);
        }

        public bool Equals(ValueDataNode? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Value == other.Value;
        }
    }
}
