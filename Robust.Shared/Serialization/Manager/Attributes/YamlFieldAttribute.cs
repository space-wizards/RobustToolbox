using System;
using JetBrains.Annotations;

namespace Robust.Shared.Serialization.Manager.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    [MeansImplicitAssignment]
    public class YamlFieldAttribute : BaseDataFieldAttribute
    {
        public readonly bool ReadOnly;
        public readonly Type? FlagType;
        public readonly Type? ConstantType;
        public readonly bool Required;
        public readonly bool ServerOnly;

        public YamlFieldAttribute([NotNull] string tag, bool readOnly = false, Type? flagType = null, Type? constType = null, int priority = 1, bool required = false, bool serverOnly = false) : base(tag, priority)
        {
            ReadOnly = readOnly;
            FlagType = flagType;
            ConstantType = constType;
            Required = required;
            ServerOnly = serverOnly;
            if (flagType != null && constType != null)
                throw new ArgumentException("Cannot have both a flagType and a constType specified");
        }
    }
}
