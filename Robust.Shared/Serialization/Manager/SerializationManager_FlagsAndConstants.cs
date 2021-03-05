using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;

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

        private Type GetFlagTypeFromTag(Type tagType)
        {
            return _flagsMapping[tagType];
        }

        private Type GetConstantTypeFromTag(Type tagType)
        {
            return _constantsMapping[tagType];
        }

        public int ReadFlag(Type tagType, DataNode node)
        {
            var flagType = GetFlagTypeFromTag(tagType);
            switch (node)
            {
                case ValueDataNode valueDataNode:
                    return int.Parse(valueDataNode.Value);
                case SequenceDataNode sequenceDataNode:
                    var flags = 0;

                    foreach (var elem in sequenceDataNode.Sequence)
                    {
                        if (elem is not ValueDataNode valueDataNode) throw new InvalidNodeTypeException();
                        flags |= (int) Enum.Parse(flagType, valueDataNode.Value);
                    }

                    return flags;
                default:
                    throw new InvalidNodeTypeException();
            }
        }

        public ValidationNode ValidateFlag(Type tagType, DataNode node)
        {
            var flagType = GetFlagTypeFromTag(tagType);
            switch (node)
            {
                case ValueDataNode valueDataNode:
                    return int.TryParse(valueDataNode.Value, out _) ? new ValidatedValueNode(node) : new ErrorNode(node, "Failed parsing flag.", false);
                case SequenceDataNode sequenceDataNode:
                    foreach (var elem in sequenceDataNode.Sequence)
                    {
                        if (elem is not ValueDataNode valueDataNode) return new ErrorNode(node, "Invalid flagtype in flag-sequence.", true);
                        if (!Enum.TryParse(flagType, valueDataNode.Value, out _)) return new ErrorNode(node, "Failed parsing flag in flag-sequence", false);
                    }

                    return new ValidatedValueNode(node);
                default:
                    return new ErrorNode(node, "Invalid nodetype for flag.", true);
            }
        }

        public int ReadConstant(Type tagType, DataNode node)
        {
            if (node is not ValueDataNode valueDataNode) throw new InvalidNodeTypeException();
            var constType = GetConstantTypeFromTag(tagType);
            return (int) Enum.Parse(constType, valueDataNode.Value);
        }

        public ValidationNode ValidateConstant(Type tagType, DataNode node)
        {
            if (node is not ValueDataNode valueDataNode) return new ErrorNode(node, "Invalid nodetype for constant.", true);
            var constType = GetConstantTypeFromTag(tagType);
            return Enum.TryParse(constType, valueDataNode.Value, out _) ? new ValidatedValueNode(node) : new ErrorNode(node, "Failed parsing constant.", false);
        }

        public DataNode WriteFlag(Type tagType, int flag)
        {
            var sequenceNode = new SequenceDataNode();
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

                    sequenceNode.Add(new ValueDataNode(flagName));
                }
            }

            return sequenceNode;
        }

        public DataNode WriteConstant(Type tagType, int constant)
        {
            var constType = GetConstantTypeFromTag(tagType);
            var constantName = Enum.GetName(constType, constant);

            if (constantName == null)
            {
                throw new InvalidOperationException($"No constant corresponding to value {constant} in {constType}.");
            }

            return new ValueDataNode(constantName);
        }
    }
}
