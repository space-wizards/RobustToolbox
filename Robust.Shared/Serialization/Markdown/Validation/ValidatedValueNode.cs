namespace Robust.Shared.Serialization.Markdown.Validation
{
    public class ValidatedValueNode : ValidatedNode
    {
        public readonly ValueDataNode ValueDataNode;
        public override bool Valid { get; }

        public ValidatedValueNode(ValueDataNode valueDataNode, bool valid)
        {
            Valid = valid;
            ValueDataNode = valueDataNode;
        }


    }
}
