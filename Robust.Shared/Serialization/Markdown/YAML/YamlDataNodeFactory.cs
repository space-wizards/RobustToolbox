namespace Robust.Shared.Serialization.Markdown.YAML
{
    public class YamlDataNodeFactory : IDataNodeFactory
    {
        public IValueDataNode GetValueNode(string value)
        {
            return new YamlValueDataNode(value);
        }

        public IMappingDataNode GetMappingNode()
        {
            return new YamlMappingDataNode();
        }

        public ISequenceDataNode GetSequenceNode()
        {
            return new YamlSequenceDataNode();
        }
    }
}
