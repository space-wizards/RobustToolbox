using System;
using JetBrains.Annotations;

namespace Robust.Shared.Serialization.Manager.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    [MeansImplicitAssignment]
    public class DataClassTargetAttribute : BaseDataFieldAttribute
    {
        public DataClassTargetAttribute([NotNull] string tag, int priority = 1) : base(tag, priority)
        {
        }
    }
}
