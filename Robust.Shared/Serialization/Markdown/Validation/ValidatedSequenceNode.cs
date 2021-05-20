using System.Collections.Generic;
using System.Linq;

namespace Robust.Shared.Serialization.Markdown.Validation
{
    public class ValidatedSequenceNode : ValidationNode
    {
        public ValidatedSequenceNode(List<ValidationNode> sequence)
        {
            Sequence = sequence;
        }

        public List<ValidationNode> Sequence { get; }

        public override bool Valid => Sequence.All(p => p.Valid);

        public override IEnumerable<ErrorNode> GetErrors()
        {
            foreach (var node in Sequence)
            {
                foreach (var invalid in node.GetErrors())
                {
                    yield return invalid;
                }
            }
        }
    }
}
