using System;
using System.Collections.Generic;
using System.Linq;

namespace Robust.Shared.Serialization.Markdown.Validation
{
    public class ValidatedMappingNode : ValidationNode
    {
        public readonly Dictionary<ValidationNode, ValidationNode> Mapping;
        public override bool Valid => Mapping.All(p => p.Key.Valid && p.Value.Valid);
        public override IEnumerable<ErrorNode> GetErrors()
        {
            foreach (var (key, value) in Mapping.Where(p => !p.Key.Valid || !p.Value.Valid))
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

        public ValidatedMappingNode(Dictionary<ValidationNode, ValidationNode> mapping)
        {
            Mapping = mapping;
        }
    }
}
