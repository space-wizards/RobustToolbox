using System.Collections.Generic;
using System.Linq;

namespace Robust.Shared.Serialization.Markdown.Validation
{
    public class ValidatedMappingNode : ValidatedNode
    {
        public readonly Dictionary<ValidatedNode, ValidatedNode> Mapping;
        public override bool Valid => Mapping.All(p => p.Key.Valid && p.Value.Valid);

        public ValidatedMappingNode(Dictionary<ValidatedNode, ValidatedNode> mapping)
        {
            Mapping = mapping;
        }
    }
}
