using System;
using JetBrains.Annotations;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.Prototypes
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
