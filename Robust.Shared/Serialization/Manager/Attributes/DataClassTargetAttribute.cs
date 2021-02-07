using System;
using JetBrains.Annotations;

namespace Robust.Shared.Prototypes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class DataClassTargetAttribute : BaseYamlField
    {
        public DataClassTargetAttribute([NotNull] string tag, int priority = 1) : base(tag, priority)
        {
        }
    }
}
