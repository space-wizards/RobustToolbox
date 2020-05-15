using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

using Robust.Shared.Interfaces.Reflection;
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

    internal sealed class FlagSerializerException : Exception
    {
        public FlagSerializerException(string message) : base(message)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Enum, AllowMultiple = true, Inherited = false)]
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

    /// <summary>
    /// <c>int</c> representation in terms of flags, decided by the tag type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">
    /// The tag type to look up the bitflag representation with. The representation enum
    /// should have a corresponding <see cref="FlagsForAttribute"/>.
    /// </typeparam>
    public class WithFlagRepresentation<T> : WithFormat<int>
    {
        private static object _staticFieldLock = new object();

        private static Type _flagType;
        public static Type FlagType => _flagType;

        private static YamlFlagSerializer _serializer;

        public WithFlagRepresentation()
        {
            lock (_staticFieldLock)
            {
                if (_serializer == null)
                {
                    var reflectionManager = IoCManager.Resolve<IReflectionManager>();

                    _flagType = null;

                    foreach (Type bitflagType in reflectionManager.FindTypesWithAttribute<FlagsForAttribute>())
                    {
                        foreach (var flagsforAttribute in bitflagType.GetCustomAttributes<FlagsForAttribute>(true))
                        {
                            if (typeof(T) == flagsforAttribute.Tag)
                            {
                                if (_flagType != null)
                                {
                                    throw new NotSupportedException($"Multiple bitflag enums declared for the tag {flagsforAttribute.Tag}.");
                                }

                                if (!bitflagType.IsEnum)
                                {
                                    throw new FlagSerializerException($"Could not create FlagSerializer for non-enum {bitflagType}.");
                                }

                                if (Enum.GetUnderlyingType(bitflagType) != typeof(int))
                                {
                                    throw new FlagSerializerException($"Could not create FlagSerializer for non-int enum {bitflagType}.");
                                }

                                if (!bitflagType.GetCustomAttributes<FlagsAttribute>(false).Any())
                                {
                                    throw new FlagSerializerException($"Could not create FlagSerializer for non-bitflag enum {bitflagType}.");
                                }


                                _flagType = bitflagType;
                            }
                        }
                    }

                    _serializer = new YamlFlagSerializer(_flagType, this);
                }
            }
        }

        public override YamlObjectSerializer.TypeSerializer GetYamlSerializer()
        {
            return _serializer;
        }

        public override Type Format => typeof(List<string>);

        public override int FromCustomFormat(object obj)
        {
            var flagNames = (List<string>)obj;
            var flags = 0;

            foreach (var flagName in flagNames)
            {
                flags |= (int)Enum.Parse(_flagType, flagName);
            }

            return flags;
        }

        public override object ToCustomFormat(int flags)
        {
            var flagNames = new List<string>();

            // Assumption: a bitflag enum has a constructor for every bit value such that
            // that bit is set in some other constructor i.e. if a 1 appears somewhere in
            // the bits of one of the enum constructors, there is an enum constructor which
            // is 1 just in that position.
            //
            // Otherwise, this code may throw an exception
            var maxFlagValue = ((int[])Enum.GetValues(_flagType)).Max();

            for(var bitIndex = 1; bitIndex <= maxFlagValue; bitIndex = bitIndex << 1)
            {
                if ((bitIndex & flags) == bitIndex)
                {
                    var flagName = Enum.GetName(_flagType, bitIndex);

                    if (flagName == null)
                    {
                        throw new FlagSerializerException($"No bitflag corresponding to bit {bitIndex} in {_flagType}, but it was set anyways.");
                    }

                    flagNames.Add(flagName);
                }
            }

            return flagNames;
        }
    }
}
