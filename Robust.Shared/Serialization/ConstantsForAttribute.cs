using System;

namespace Robust.Shared.Serialization
{
    /// <summary>
    /// Attribute for marking an enum type as being the constant representation for a field.
    ///
    /// Some fields are arbitrary ints, but it's helpful for readability to have them be
    /// named constants instead. This allows for that.
    ///
    /// NB: AllowMultiple is <c>true</c> - don't assume the same representation cannot
    /// be reused between multiple fields.
    /// </summary>
    [AttributeUsage(AttributeTargets.Enum, AllowMultiple = true, Inherited = false)]
    public class ConstantsForAttribute : Attribute
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
        public ConstantsForAttribute(Type tag)
        {
            _tag = tag;
        }
    }
}
