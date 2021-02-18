using System;
using Robust.Shared.Serialization.Markdown.YAML;
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
        }

        public string Value { get; set; }

        public override DataNode Copy()
        {
            return new ValueDataNode(Value);
        }

        public override bool Equals(object? obj)
        {
            if(obj is not ValueDataNode node) return base.Equals(obj);
            return node.Value == Value;
        }
    }
}
