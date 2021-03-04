using System;
using System.Collections.Generic;
using System.Linq;

namespace Robust.Shared.Serialization.Markdown.Validation
{
    public class ValidatedSequenceNode : ValidationNode
    {
        public readonly List<ValidationNode> Sequence;

        public override bool Valid => Sequence.All(p => p.Valid);
        public override IEnumerable<ErrorNode> GetErrors()
        {
            for (int i = 0; i < Sequence.Count; i++)
            {
                foreach (var invalid in Sequence[i].GetErrors())
                {
                    yield return invalid;
                }
            }
        }

        public ValidatedSequenceNode(List<ValidationNode> sequence)
        {
            Sequence = sequence;
        }
    }
}
