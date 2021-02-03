using System;

namespace Robust.Shared.Prototypes
{
    public abstract class BaseYamlField : Attribute
    {
        public readonly string Tag;
        public readonly int Priority;

        public BaseYamlField(string tag, int priority = 1)
        {
            Tag = tag;
            Priority = priority;
        }
    }
}
