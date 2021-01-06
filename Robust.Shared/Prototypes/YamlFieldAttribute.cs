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

        public YamlFieldAttribute([NotNull] string tag, bool readOnly = false, Type? flagType = null)
        {
            Tag = tag;
            ReadOnly = readOnly;
            FlagType = flagType;
        }
    }
}
