using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Serialization.Markdown
{
    public class ValueDataNode : DataNode<ValueDataNode>
    {
        public ValueDataNode(string value) : base(DataPosition.Invalid, DataPosition.Invalid)
        {
            Value = value;
        }

        public ValueDataNode(YamlScalarNode node) : base(node.Start, node.End)
        {
            Value = node.Value ?? "";
            Tag = node.Tag;
        }

        public string Value { get; set; }

        public override ValueDataNode Copy()
        {
            return new ValueDataNode(Value)
            {
                Tag = Tag,
                Start = Start,
                End = End
            };
        }

        public override ValueDataNode? Except(ValueDataNode node)
        {
            if (node.Value == Value) return null;
            return Copy();
        }

        public override bool Equals(object? obj)
        {
            if(obj is not ValueDataNode node) return false;
            return node.Value == Value;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString()
        {
            return Value;
        }
    }
}
