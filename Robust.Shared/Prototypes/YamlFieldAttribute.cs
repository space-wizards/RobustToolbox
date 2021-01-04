using System;
using JetBrains.Annotations;

namespace Robust.Shared.Prototypes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class YamlFieldAttribute : Attribute
    {
        public readonly string Tag;

        public YamlFieldAttribute([NotNull] string tag, bool readOnly = false) //todo Paul: readonly
        {
            Tag = tag;
        }
    }
}
