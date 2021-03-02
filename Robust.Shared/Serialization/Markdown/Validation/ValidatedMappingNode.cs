using System;
using System.Collections.Generic;
using System.Linq;

namespace Robust.Shared.Serialization.Markdown.Validation
{
    public class ValidatedMappingNode : ValidatedNode
    {
        public readonly Dictionary<ValidatedNode, ValidatedNode> Mapping;
        public override bool Valid => Mapping.All(p => p.Key.Valid && p.Value.Valid);
        public override IEnumerable<string> Invalids()
        {
            foreach (var (key, value) in Mapping.Where(p => !p.Key.Valid || !p.Value.Valid))
            {
                if (!key.Valid)
                {
                    foreach (var invalid in key.Invalids())
                    {
                        yield return invalid;
                    }
                }
                else if (!value.Valid)
                {
                    foreach (var invalid in value.Invalids())
                    {
                        yield return $"[{key}] <> {invalid}";
                    }
                }
            }
        }

        public ValidatedMappingNode(Dictionary<ValidatedNode, ValidatedNode> mapping)
        {
            Mapping = mapping;
        }
    }
}
