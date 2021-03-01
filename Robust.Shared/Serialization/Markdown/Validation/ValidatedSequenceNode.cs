using System.Collections.Generic;
using System.Linq;

namespace Robust.Shared.Serialization.Markdown.Validation
{
    public class ValidatedSequenceNode : ValidatedNode
    {
        public readonly List<ValidatedNode> Sequence;

        public override bool Valid => Sequence.All(p => p.Valid);

        public ValidatedSequenceNode(List<ValidatedNode> sequence)
        {
            Sequence = sequence;
        }
    }
}
