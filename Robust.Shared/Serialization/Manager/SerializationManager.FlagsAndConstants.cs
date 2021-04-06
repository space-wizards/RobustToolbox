using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Robust.Shared.Serialization.Manager
{
    public partial class SerializationManager
    {
        private readonly Dictionary<Type, Type> _constantsMapping = new();
        private readonly Dictionary<Type, Type> _flagsMapping = new();

        private void InitializeFlagsAndConstants()
        {
            foreach (Type constType in _reflectionManager.FindTypesWithAttribute<ConstantsForAttribute>())
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

            foreach (var bitflagType in _reflectionManager.FindTypesWithAttribute<FlagsForAttribute>())
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
                }
            }
        }

        public Type GetFlagTypeFromTag(Type tagType)
        {
            return _flagsMapping[tagType];
        }

        public Type GetConstantTypeFromTag(Type tagType)
        {
            return _constantsMapping[tagType];
        }
    }
}
