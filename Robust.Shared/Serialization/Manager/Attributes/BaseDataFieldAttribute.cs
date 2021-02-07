using System;

namespace Robust.Shared.Serialization.Manager.Attributes
{
    public class BaseDataFieldAttribute : Attribute
    {
        public readonly string Tag;
        public readonly int Priority;

        public BaseDataFieldAttribute(string tag, int priority = 1)
        {
            Tag = tag;
            Priority = priority;
        }
    }
}
