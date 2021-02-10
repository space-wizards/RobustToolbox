using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;
using Robust.Shared.IoC;

namespace Robust.Shared.Serialization
{
    /// <summary>
    /// Serializer for converting integer to/from named bitflag representation in YAML.
    ///
    /// This allows for serializing/deserializing integer values as list of bitflag
    /// names, to give a readable YAML representation. It also supports plain integer
    /// values in the YAML for reading.
    /// </summary>
    internal class YamlFlagSerializer : YamlCustomFormatSerializer<int>
    {
        private readonly Type _flagType;

        /// <summary>
        /// Create a YamlFlagSerializer using the given bitflag representation type.
        /// </summary>
        /// <param name="type">
        /// The bitflag enum for which the constructors will be used to represent
        /// bits being set in the integer value.
        /// </param>
        /// <exception cref="FlagSerializerException">
        /// Thrown if the bitflag type is not a <c>enum</c>, or does not have the
        /// <see cref="System.Flags"/> attribute.
        /// </exception>
        public YamlFlagSerializer(Type type, WithFormat<int> formatter) : base(formatter)
        {
            _flagType = type;
        }

        /// <summary>
        /// Turn a YAML node into an integer value with the correct flags set.
        /// </summary>
        public override object NodeToType(Type type, YamlNode node, YamlObjectSerializer objectSerializer)
        {
            // Fallback to just a number, if it's not in flag format yet
            // This is a hack, but it's only for legacy representations, so it's not so bad
            if (node is YamlScalarNode)
            {
                return (int)objectSerializer.NodeToType(typeof(int), node);
            }

            return base.NodeToType(type, node, objectSerializer);
        }

        /// <summary>
        /// Turn bitflags into a YAML node with the corresponding constructors.
        /// </summary>
        /// <returns>
        /// A sequence node of the flag names if the flags are non-zero, or the
        /// sequence node of the zero flag name if it is defined on the representation
        /// type. Otherwise, the scalar node 0.
        /// </returns>
        /// <exception cref="FlagSerializerException">
        /// Thrown if the serializer encounters a bit set in a position for which it
        /// cannot find a corresponding constructor.
        /// </exception>
        public override YamlNode TypeToNode(object obj, YamlObjectSerializer objectSerializer)
        {
            var flags = (int)obj;

            if (flags == 0)
            {
                var zeroName = Enum.GetName(_flagType, 0);

                // If there's no name for 0, just write 0
                if (zeroName == null)
                {
                    return objectSerializer.TypeToNode(0);
                } else {
                    return objectSerializer.TypeToNode(new List<string> { zeroName });
                }

            } else {
                return base.TypeToNode(flags, objectSerializer);
            }
        }
    }

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
    public class FlagsForAttribute : Attribute
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
