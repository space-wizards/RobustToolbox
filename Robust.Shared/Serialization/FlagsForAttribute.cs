using System;

namespace Robust.Shared.Serialization
{
    /// <summary>
    /// Attribute for marking an enum type as being the bitflag representation for a field.
    ///
    /// Some int values in the engine are bitflags, but the actual bitflag definitions
    /// are reserved for the content layer. This means that serialization/deserialization
    /// of those flags into readable YAML is impossible, unless the engine is notified
    /// that a certain representation should be used. That's the role of this attribute.
    ///
    /// NB: AllowMultiple is <c>true</c> - don't assume the same representation cannot
    /// be reused between multiple fields.
    /// </summary>
    [AttributeUsage(AttributeTargets.Enum, AllowMultiple = true, Inherited = false)]
    public sealed class FlagsForAttribute : Attribute
    {
        private readonly Type _tag;
        public Type Tag => _tag;

        // NB: This is not generic because C# does not allow generic attributes

        /// <summary>
        /// An attribute with tag type <paramref name="tag"/>
        /// </summary>
        /// <param name="tag">
        /// An arbitrary tag type used for coordinating between the data field and the
        /// representation. Not actually used for serialization/deserialization.
        /// </param>
        public FlagsForAttribute(Type tag)
        {
            _tag = tag;
        }
    }
}
