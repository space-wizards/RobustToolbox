using System;
using JetBrains.Annotations;

namespace Robust.Shared.Serialization.Manager.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    [MeansImplicitAssignment]
    public class YamlFieldWithFlagAttribute : YamlFieldAttribute
    {
        public readonly Type FlagTag;
        public YamlFieldWithFlagAttribute([NotNull] string tag, Type flagTag, bool readOnly = false, int priority = 1, bool required = false, bool serverOnly = false) : base(tag, readOnly, priority, required, serverOnly)
        {
            FlagTag = flagTag;
        }
    }
}
