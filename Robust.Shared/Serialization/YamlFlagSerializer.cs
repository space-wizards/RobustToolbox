using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Serialization
{
    internal class YamlFlagSerializer
    {
        private readonly Type _flagType;

        public YamlFlagSerializer(Type type)
        {
            if (!type.IsEnum)
            {
                throw new FlagSerializerException($"Could not create FlagSerializer for non-enum type {type}.");
            }

            if (!type.GetCustomAttributes(typeof(FlagsAttribute), false).Any())
            {
                throw new FlagSerializerException($"Could not create FlagSerializer for non-bitflag enum {type}.");
            }

            _flagType = type;
        }

        public int NodeToFlags(YamlNode node, YamlObjectSerializer objectSerializer)
        {
            // Fallback to just a number, if it's not in flag format yet
            // This is a hack, but it's only for legacy representations, so it's not so bad
            if (node is YamlScalarNode)
            {
                return (int)objectSerializer.NodeToType(typeof(int), null, node);
            }


            var flagNames = (List<string>)objectSerializer.NodeToType(typeof(List<string>), null, node);
            var flags = 0;

            foreach (var flagName in flagNames)
            {
                flags |= (int)Enum.Parse(_flagType, flagName);
            }

            return flags;
        }

        public YamlNode FlagsToNode(int flags, YamlObjectSerializer objectSerializer)
        {
            var flagNames = new List<string>();

            if (flags == 0)
            {
                var zeroName = Enum.GetName(_flagType, 0);

                // If there's no name for 0, just write 0
                if (zeroName == null)
                {
                    return objectSerializer.TypeToNode(0, null);
                }

                flagNames.Add(zeroName);
            } else {
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
            }

            return objectSerializer.TypeToNode(flagNames, null);
        }
    }

    internal sealed class FlagSerializerException : Exception
    {
        public FlagSerializerException(string message) : base(message)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Enum, AllowMultiple = true, Inherited = false)]
    public class FlagsForAttribute : Attribute
    {
        private readonly string _fieldName;
        public string FieldName => _fieldName;

        public FlagsForAttribute(string fieldName)
        {
            _fieldName = fieldName;
        }
    }
}
