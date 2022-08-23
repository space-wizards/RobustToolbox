using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Robust.Shared.Serialization.Manager
{
    public partial class SerializationManager
    {
        private readonly Dictionary<Type, Type> _flagsMapping = new();
        private readonly Dictionary<Type, int> _highestFlagBit = new();

        private readonly Dictionary<Type, Type> _constantsMapping = new();

        private void InitializeFlagsAndConstants(IEnumerable<Type> flags, IEnumerable<Type> constants)
        {
            foreach (Type constType in constants)
            {
                if (!constType.IsEnum)
                {
                    throw new InvalidOperationException($"Could not create ConstantMapping for non-enum {constType}.");
                }

                if (Enum.GetUnderlyingType(constType) != typeof(int))
                {
                    throw new InvalidOperationException($"Could not create ConstantMapping for non-int enum {constType}.");
                }

                foreach (var constantsForAttribute in constType.GetCustomAttributes<ConstantsForAttribute>(true))
                {
                    if (_constantsMapping.ContainsKey(constantsForAttribute.Tag))
                    {
                        throw new NotSupportedException($"Multiple constant enums declared for the tag {constantsForAttribute.Tag}.");
                    }

                    _constantsMapping.Add(constantsForAttribute.Tag, constType);
                }
            }

            foreach (var bitflagType in flags)
            {
                if (!bitflagType.IsEnum)
                {
                    throw new InvalidOperationException($"Could not create FlagSerializer for non-enum {bitflagType}.");
                }

                if (Enum.GetUnderlyingType(bitflagType) != typeof(int))
                {
                    throw new InvalidOperationException($"Could not create FlagSerializer for non-int enum {bitflagType}.");
                }

                if (!bitflagType.GetCustomAttributes<FlagsAttribute>(false).Any())
                {
                    throw new InvalidOperationException($"Could not create FlagSerializer for non-bitflag enum {bitflagType}.");
                }

                foreach (var flagType in bitflagType.GetCustomAttributes<FlagsForAttribute>(true))
                {
                    if (_flagsMapping.ContainsKey(flagType.Tag))
                    {
                        throw new NotSupportedException($"Multiple bitflag enums declared for the tag {flagType.Tag}.");
                    }

                    _flagsMapping.Add(flagType.Tag, bitflagType);

                    var highestBit = bitflagType
                        .GetEnumValues()
                        .Cast<int>()
                        .Select(value => Convert.ToString(value, 2))
                        .Max(s => s.Length);

                    _highestFlagBit.Add(flagType.Tag, highestBit);
                }
            }
        }

        public Type GetFlagTypeFromTag(Type tagType)
        {
            return _flagsMapping[tagType];
        }

        public int GetFlagHighestBit(Type tagType)
        {
            return _highestFlagBit[tagType];
        }

        public Type GetConstantTypeFromTag(Type tagType)
        {
            return _constantsMapping[tagType];
        }
    }
}
