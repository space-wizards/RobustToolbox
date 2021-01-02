using System;

namespace Robust.Shared.Prototypes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class CustomYamlTargetAttribute : Attribute
    {
        public string Tag;

        public CustomYamlTargetAttribute(string tag)
        {
            Tag = tag;
        }
    }
}
