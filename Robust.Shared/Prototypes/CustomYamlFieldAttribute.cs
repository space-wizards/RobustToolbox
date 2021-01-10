using System;

namespace Robust.Shared.Prototypes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class CustomYamlFieldAttribute : Attribute
    {
        public string Tag;
        public int Priority;

        public CustomYamlFieldAttribute(string tag, int priority = 1)
        {
            Tag = tag;
            Priority = priority;
        }
    }
}
