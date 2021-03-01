using System;
using JetBrains.Annotations;

namespace Robust.Shared.Serialization.Manager.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    [MeansImplicitAssignment]
    public class DataFieldAttribute : Attribute
    {
        public readonly string Tag;
        public readonly int Priority;
        public readonly bool ReadOnly;
        public readonly bool Required;
        public readonly bool ServerOnly;

        public DataFieldAttribute([NotNull] string tag, bool readOnly = false, int priority = 1, bool required = false, bool serverOnly = false)
        {
            Tag = tag;
            Priority = priority;
            ReadOnly = readOnly;
            Required = required;
            ServerOnly = serverOnly;
        }
    }
}
