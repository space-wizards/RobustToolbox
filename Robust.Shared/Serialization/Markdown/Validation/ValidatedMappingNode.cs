using System.Collections.Generic;
using System.Linq;

namespace Robust.Shared.Serialization.Markdown.Validation
{
    public sealed class ValidatedMappingNode : ValidationNode
    {
        public ValidatedMappingNode(Dictionary<ValidationNode, ValidationNode> mapping)
        {
            Mapping = mapping;
        }

        public Dictionary<ValidationNode, ValidationNode> Mapping { get; }

        public override bool Valid => Mapping.All(p => p.Key.Valid && p.Value.Valid);

        public override IEnumerable<ErrorNode> GetErrors()
        {
            var errors = Mapping.Where(p => !p.Key.Valid || !p.Value.Valid);

            foreach (var (key, value) in errors)
            {
                foreach (var invalid in key.GetErrors())
                {
                    yield return invalid;
                }

                foreach (var invalid in value.GetErrors())
                {
                    yield return invalid;
                }
            }
        }
    }
}
