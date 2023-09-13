using System;
using JetBrains.Annotations;

namespace Robust.Shared.Serialization.Manager.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    [MeansImplicitAssignment]
    [MeansImplicitUse(ImplicitUseKindFlags.Assign)]
    [Virtual]
    public class DataFieldAttribute : DataFieldBaseAttribute
    {
        /// <summary>
        ///     The name of this field in YAML.
        ///     If null, the name of the C# field will be used instead, with the first letter lowercased.
        /// </summary>
        public string? Tag { get; internal set; }

        /// <summary>
        ///     Whether or not this field being mapped is required for the component to function.
        ///     This will not guarantee that the field is mapped when the program is run,
        ///     it is meant to be used as metadata information.
        /// </summary>
        public readonly bool Required;

        public DataFieldAttribute(string? tag = null, bool readOnly = false, int priority = 1, bool required = false, bool serverOnly = false, Type? customTypeSerializer = null) : base(readOnly, priority, serverOnly, customTypeSerializer)
        {
            Tag = tag;
            Required = required;
        }

        public override string? ToString()
        {
            return Tag;
        }
    }

    public abstract class DataFieldBaseAttribute : Attribute
    {
        public readonly int Priority;
        public readonly Type? CustomTypeSerializer;
        public readonly bool ReadOnly;
        public readonly bool ServerOnly;

        protected DataFieldBaseAttribute(bool readOnly = false, int priority = 1, bool serverOnly = false, Type? customTypeSerializer = null)
        {
            ReadOnly = readOnly;
            Priority = priority;
            ServerOnly = serverOnly;
            CustomTypeSerializer = customTypeSerializer;
        }
    }

}
