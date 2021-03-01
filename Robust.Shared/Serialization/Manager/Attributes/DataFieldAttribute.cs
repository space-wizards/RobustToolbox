using System;
using JetBrains.Annotations;

namespace Robust.Shared.Serialization.Manager.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    [MeansImplicitAssignment]
    public class DataFieldAttribute : BaseDataFieldAttribute
    {
        public readonly bool ReadOnly;

        /// <summary>
        ///     Whether or not this field being mapped is required for the component to function.
        ///     This will not guarantee that the field is mapped when the program is run,
        ///     it is meant to be used as metadata information.
        /// </summary>
        public readonly bool Required;

        public readonly bool ServerOnly;

        public DataFieldAttribute([NotNull] string tag, bool readOnly = false, int priority = 1, bool required = false, bool serverOnly = false) : base(tag, priority)
        {
            ReadOnly = readOnly;
            Required = required;
            ServerOnly = serverOnly;
        }
    }
}
