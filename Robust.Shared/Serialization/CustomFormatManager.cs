using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.IoC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Robust.Shared.Serialization
{
    /// <inheritdoc cref="ICustomFormatManager"/>
    public class CustomFormatManager : ICustomFormatManager
    {
        private Dictionary<Type, WithFormat<int>> _flagFormatters = new Dictionary<Type, WithFormat<int>>();

        public WithFormat<int> FlagFormat<T>()
        {
            if (!_flagFormatters.TryGetValue(typeof(T), out var formatter))
            {
                formatter = new WithFlagRepresentation(GetFlag<T>());
                _flagFormatters.Add(typeof(T), formatter);
            }

            return formatter;
        }

        /// <summary>
        /// Get the enum flag type for the given tag <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">
        /// The tag type to use for finding the flag representation. To learn more,
        /// see the <see cref="FlagsForAttribute"/>.
        /// </typeparam>
        /// <exception cref="FlagSerializerException">
        /// Thrown if:
        /// <list type="bullet">
        /// <item>
        /// <description>The tag type corresponds to no enum flag representation.</description>
        /// </item>
        /// <item>
        /// <description>The tag type corresponds to more than one enum flag representation.</description>
        /// </item>
        /// <item>
        /// <description>The tag type corresponds to a non-enum representation.</description>
        /// </item>
        /// <item>
        /// <description>The tag type corresponds to a non-int enum representation.</description>
        /// </item>
        /// <item>
        /// <description>The tag type corresponds to a non-bitflag int enum representation.</description>
        /// </item>
        /// </list>
        /// </exception>
        /// <returns>
        /// The unique int-backed bitflag enum type for the given tag.
        /// </returns>
        private Type GetFlag<T>()
        {
            var reflectionManager = IoCManager.Resolve<IReflectionManager>();

            Type flagType = null;

            foreach (Type bitflagType in reflectionManager.FindTypesWithAttribute<FlagsForAttribute>())
            {
                foreach (var flagsforAttribute in bitflagType.GetCustomAttributes<FlagsForAttribute>(true))
                {
                    if (typeof(T) == flagsforAttribute.Tag)
                    {
                        if (flagType != null)
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


                        flagType = bitflagType;
                    }
                }
            }

            if (flagType == null)
            {
                throw new FlagSerializerException($"Found no type marked with a `FlagsForAttribute(typeof({typeof(T)}))`.");
            }

            return flagType;
        }
    }

    /// <summary>
    /// <c>int</c> representation in terms of some enum flag type.
    /// </summary>
    public class WithFlagRepresentation : WithFormat<int>
    {
        private Type _flagType;
        public Type FlagType => _flagType;

        private YamlFlagSerializer _serializer;

        public WithFlagRepresentation(Type flagType)
        {
            _flagType = flagType;
            _serializer = new YamlFlagSerializer(_flagType, this);

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

            for (var bitIndex = 1; bitIndex <= maxFlagValue; bitIndex = bitIndex << 1)
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

    internal sealed class FlagSerializerException : Exception
    {
        public FlagSerializerException(string message) : base(message)
        {
        }
    }
}
