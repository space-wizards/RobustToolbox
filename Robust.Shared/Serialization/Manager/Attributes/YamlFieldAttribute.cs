using System;
using JetBrains.Annotations;

namespace Robust.Shared.Serialization.Manager.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    [MeansImplicitAssignment]
    public class YamlFieldAttribute : BaseDataFieldAttribute
    {
        public readonly bool ReadOnly;
        public readonly bool Required;
        public readonly bool ServerOnly;

        public YamlFieldAttribute([NotNull] string tag, bool readOnly = false, int priority = 1, bool required = false, bool serverOnly = false) : base(tag, priority)
        {
            ReadOnly = readOnly;
            Required = required;
            ServerOnly = serverOnly;
        }
    }
}
