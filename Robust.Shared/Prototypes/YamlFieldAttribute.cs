using System;

namespace Robust.Shared.Prototypes
{
    public class YamlFieldAttribute : Attribute
    {
        public readonly string Tag;

        public YamlFieldAttribute(string tag)
        {
            Tag = tag;
        }
    }
}
