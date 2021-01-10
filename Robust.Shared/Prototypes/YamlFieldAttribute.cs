using System;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.Serialization;

namespace Robust.Shared.Prototypes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class YamlFieldAttribute : Attribute
    {
        public readonly string Tag;
        public readonly bool ReadOnly;
        public readonly Type? FlagType;
        public readonly Type? ConstantType;
        public readonly int Priority;

        public YamlFieldAttribute([NotNull] string tag, bool readOnly = false, Type? flagType = null, Type? constType = null, int priority = 1)
        {
            Tag = tag;
            Priority = priority;
            ReadOnly = readOnly;
            FlagType = flagType;
            ConstantType = constType;
            if (FlagType != null && constType != null)
                throw new ArgumentException("Cannot have both a flagType and a constType specified");
        }
    }
}
