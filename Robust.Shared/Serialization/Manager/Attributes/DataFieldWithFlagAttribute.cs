using System;
using JetBrains.Annotations;

namespace Robust.Shared.Serialization.Manager.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    [MeansImplicitAssignment]
    public class DataFieldWithFlagAttribute : DataFieldAttribute
    {
        public readonly Type FlagTag;
        public DataFieldWithFlagAttribute([NotNull] string tag, Type flagTag, bool readOnly = false, int priority = 1, bool required = false, bool serverOnly = false) : base(tag, readOnly, priority, required, serverOnly)
        {
            FlagTag = flagTag;
        }
    }
}
