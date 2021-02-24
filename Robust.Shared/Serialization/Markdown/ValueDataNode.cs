using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Serialization.Markdown
{
    public class ValueDataNode : DataNode
    {
        public ValueDataNode(string value)
        {
            Value = value;
        }

        public ValueDataNode(YamlScalarNode node)
        {
            Value = node.Value ?? "";
            Tag = node.Tag;
        }

        public string Value { get; set; }

        public override DataNode Copy()
        {
            return new ValueDataNode(Value) {Tag = Tag};
        }

        public override bool Equals(object? obj)
        {
            if(obj is not ValueDataNode node) return base.Equals(obj);
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
