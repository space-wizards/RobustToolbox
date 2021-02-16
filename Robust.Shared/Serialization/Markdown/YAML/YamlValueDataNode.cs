using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Serialization.Markdown.YAML
{
    public class YamlValueDataNode : IValueDataNode
    {
        public string Value;

        public YamlValueDataNode(string value)
        {
            Value = value;
        }

        public YamlValueDataNode(YamlScalarNode node)
        {
            Value = node.Value ?? "";
        }

        public string GetValue() => Value;
        public IDataNode Copy()
        {
            return new YamlValueDataNode(Value);
        }

        public override bool Equals(object? obj)
        {
            if(obj is not YamlValueDataNode node) return base.Equals(obj);
            return node.Value == Value;
        }
    }
}
