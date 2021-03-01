namespace Robust.Shared.Serialization.Markdown.Validation
{
    public class ErrorNode : ValidatedNode
    {
        public readonly DataNode Node;

        public ErrorNode(DataNode node)
        {
            Node = node;
        }

        public override bool Valid => false;
    }
}
