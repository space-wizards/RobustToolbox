using System;

namespace Robust.Shared.Prototypes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class CustomYamlFieldAttribute : Attribute
    {
        public string Tag;

        public CustomYamlFieldAttribute(string tag)
        {
            Tag = tag;
        }
    }
}
