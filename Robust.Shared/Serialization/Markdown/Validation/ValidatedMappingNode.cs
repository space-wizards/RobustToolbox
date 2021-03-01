namespace Robust.Shared.Serialization.Markdown.Validation
{
    public class ValidatedMappingNode : ValidatedNode
    {
        public readonly MappingDataNode MappingDataNode;
        public override bool Valid { get; }

        public ValidatedMappingNode(MappingDataNode mappingDataNode, bool valid)
        {
            MappingDataNode = mappingDataNode;
            Valid = valid;
        }
    }
}
