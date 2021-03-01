namespace Robust.Shared.Serialization.Markdown.Validation
{
    public class ValidatedValueNode : ValidatedNode
    {
        public readonly DataNode DataNode;
        public override bool Valid => true;

        public ValidatedValueNode(DataNode dataNode)
        {
            DataNode = dataNode;
        }
    }
}
