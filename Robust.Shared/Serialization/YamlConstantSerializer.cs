using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.IoC;

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

        /// <summary>
        /// Create a YamlConstantSerializer using the given bitflag representation type.
        /// </summary>
        /// <param name="type">
        /// The enum for which the constructors will be used to represent constant values.
        /// </param>
        /// </exception>
        public YamlConstantSerializer(Type type, WithFormat<int> formatter) : base(formatter)
        {
            _constantType = type;
        }

        /// <summary>
        /// Turn a YAML node into an integer value of the corresponding constant.
        /// </summary>
        public override object NodeToType(Type type, YamlNode node, YamlObjectSerializer objectSerializer)
        {
            try
            {
                // First try to deserialize a legacy integer value
                return (int)objectSerializer.NodeToType(typeof(int), node);
            }
            catch (ArgumentException e)
            {
                return base.NodeToType(type, node, objectSerializer);
            }

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
