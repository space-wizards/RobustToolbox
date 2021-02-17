using System;
using JetBrains.Annotations;

namespace Robust.Shared.Serialization.Manager.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    [MeansImplicitAssignment]
    public class DataFieldWithConstantAttribute : DataFieldAttribute
    {
        public readonly Type ConstantTag;
        public DataFieldWithConstantAttribute([NotNull] string tag, Type constantTag, bool readOnly = false, int priority = 1, bool required = false, bool serverOnly = false) : base(tag, readOnly, priority, required, serverOnly)
        {
            ConstantTag = constantTag;
        }
    }
}
