using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Serialization.Markdown;
using System.Reflection;

namespace Robust.Shared.Serialization.Manager
{
    public partial class Serv3Manager
    {
        private Dictionary<Type, Type> _constantsMapping = new();
        private Dictionary<Type, Type> _flagsMapping = new();

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

        private Type GetFlagTypeFromTag(Type tagType)
        {
            return _flagsMapping[tagType];
        }

        private Type GetConstantTypeFromTag(Type tagType)
        {
            return _constantsMapping[tagType];
        }

        public int ReadFlag(Type tagType, IDataNode node)
        {
            var flagType = GetFlagTypeFromTag(tagType);
            switch (node)
            {
                case IValueDataNode valueDataNode:
                    return int.Parse(valueDataNode.GetValue());
                case ISequenceDataNode sequenceDataNode:
                    var flags = 0;

                    foreach (var elem in sequenceDataNode.Sequence)
                    {
                        if (elem is not IValueDataNode valueDataNode) throw new InvalidNodeTypeException();
                        flags |= (int)Enum.Parse(flagType, valueDataNode.GetValue());
                    }

                    return flags;
                default:
                    throw new InvalidNodeTypeException();
            }
        }

        public int ReadConstant(Type tagType, IDataNode node)
        {
            if (node is not IValueDataNode valueDataNode) throw new InvalidNodeTypeException();
            var constType = GetConstantTypeFromTag(tagType);
            return (int) Enum.Parse(constType, valueDataNode.GetValue());
        }

        public IDataNode WriteFlag(Type tagType, int flag, IDataNodeFactory nodeFactory)
        {
            var sequenceNode = nodeFactory.GetSequenceNode();
            var flagType = GetFlagTypeFromTag(tagType);

            // Assumption: a bitflag enum has a constructor for every bit value such that
            // that bit is set in some other constructor i.e. if a 1 appears somewhere in
            // the bits of one of the enum constructors, there is an enum constructor which
            // is 1 just in that position.
            //
            // Otherwise, this code may throw an exception
            var maxFlagValue = ((int[])Enum.GetValues(flagType)).Max();

            for (var bitIndex = 1; bitIndex <= maxFlagValue; bitIndex = bitIndex << 1)
            {
                if ((bitIndex & flag) == bitIndex)
                {
                    var flagName = Enum.GetName(flagType, bitIndex);

                    if (flagName == null)
                    {
                        throw new InvalidOperationException($"No bitflag corresponding to bit {bitIndex} in {flagType}, but it was set anyways.");
                    }

                    sequenceNode.Add(nodeFactory.GetValueNode(flagName));
                }
            }

            return sequenceNode;
        }

        public IDataNode WriteConstant(Type tagType, int constant, IDataNodeFactory nodeFactory)
        {
            var constType = GetConstantTypeFromTag(tagType);
            var constantName = Enum.GetName(constType, constant);

            if (constantName == null)
            {
                throw new InvalidOperationException($"No constant corresponding to value {constant} in {constType}.");
            }

            return nodeFactory.GetValueNode(constantName);
        }
    }
}
