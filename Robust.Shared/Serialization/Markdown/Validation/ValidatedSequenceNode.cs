using System;
using System.Collections.Generic;
using System.Linq;

namespace Robust.Shared.Serialization.Markdown.Validation
{
    public class ValidatedSequenceNode : ValidatedNode
    {
        public readonly List<ValidatedNode> Sequence;

        public override bool Valid => Sequence.All(p => p.Valid);
        public override IEnumerable<string> Invalids()
        {
            for (int i = 0; i < Sequence.Count; i++)
            {
                var entry = Sequence[i];
                if(entry.Valid) continue;

                foreach (var invalid in entry.Invalids())
                {
                    yield return $"[{i}] <> {invalid}";
                }
            }
        }

        public ValidatedSequenceNode(List<ValidatedNode> sequence)
        {
            Sequence = sequence;
        }
    }
}
