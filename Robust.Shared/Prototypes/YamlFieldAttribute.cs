using System;
using JetBrains.Annotations;

namespace Robust.Shared.Prototypes
{
    public class YamlFieldAttribute : Attribute
    {
        public readonly string Tag;

        public YamlFieldAttribute([NotNull] string tag)
        {
            Tag = tag;
        }
    }
}
