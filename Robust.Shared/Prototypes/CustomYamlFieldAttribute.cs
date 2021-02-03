using System;
using JetBrains.Annotations;

namespace Robust.Shared.Prototypes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class CustomYamlFieldAttribute : BaseYamlField
    {
        public CustomYamlFieldAttribute([NotNull] string tag, int priority = 1) : base(tag, priority)
        {
        }
    }
}
