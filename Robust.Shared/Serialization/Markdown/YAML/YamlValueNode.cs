using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Serialization.Markdown.YAML
{
    public class YamlValueDataNode : IValueDataNode
    {
        public readonly string Value;

        public YamlValueDataNode(string value)
        {
            Value = value;
        }

        public YamlValueDataNode(YamlScalarNode node)
        {
            Value = node.Value ?? "";
        }

        public string GetValue() => Value;
    }
}
