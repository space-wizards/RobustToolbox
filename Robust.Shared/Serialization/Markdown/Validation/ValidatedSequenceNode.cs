namespace Robust.Shared.Serialization.Markdown.Validation
{
    public class ValidatedSequenceNode : ValidatedNode
    {
        public readonly SequenceDataNode SequenceDataNode;

        public override bool Valid { get; }

        public ValidatedSequenceNode(SequenceDataNode sequenceDataNode, bool valid)
        {
            SequenceDataNode = sequenceDataNode;
            Valid = valid;
        }
    }
}
