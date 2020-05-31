using System;
using YamlDotNet.RepresentationModel;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization
{
    /// <summary>
    /// Serializer for converting integer to/from named constant representation in YAML.
    ///
    /// This serializes an integer value as a named constant, based on the values of some
    /// enum type.
    /// </summary>
    internal class YamlConstantSerializer : YamlCustomFormatSerializer<int>
    {
        private readonly Type _constantType;
        private readonly WithConstantRepresentation _formatter;

        /// <summary>
        /// Create a YamlConstantSerializer using the given bitflag representation type.
        /// </summary>
        /// <param name="type">
        /// The enum for which the constructors will be used to represent constant values.
        /// </param>
        /// </exception>
        public YamlConstantSerializer(Type type, WithConstantRepresentation formatter) : base(formatter)
        {
            _constantType = type;
            _formatter = formatter;
        }

        /// <summary>
        /// Turn a YAML node into an integer value of the corresponding constant.
        /// </summary>
        public override object NodeToType(Type type, YamlNode node, YamlObjectSerializer objectSerializer)
        {
            if (!(node is YamlScalarNode scalar))
            {
                throw new FormatException("Constant must be a YAML scalar.");
            }

            return _formatter.FromCustomFormatText(scalar.AsString());
        }

        /// <summary>
        /// Turn an integer into a YAML node with the corresponding constant name.
        /// </summary>
        /// <returns>
        /// A string node of the constant corresponding to the given value.
        /// </returns>
        /// <exception cref="ConstantSerializerException">
        /// Thrown if there is no corresponding constant name to the value passed in.
        /// </exception>
        public override YamlNode TypeToNode(object obj, YamlObjectSerializer objectSerializer)
        {
            return base.TypeToNode(obj, objectSerializer);
        }
    }

    [AttributeUsage(AttributeTargets.Enum, AllowMultiple = true, Inherited = false)]
    /// <summary>
    /// Attribute for marking an enum type as being the constant representation for a field.
    ///
    /// Some fields are arbitrary ints, but it's helpful for readability to have them be
    /// named constants instead. This allows for that.
    ///
    /// NB: AllowMultiple is <c>true</c> - don't assume the same representation cannot
    /// be reused between multiple fields.
    /// </summary>
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
