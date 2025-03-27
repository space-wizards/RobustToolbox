using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations.Generic
{
    [TypeSerializer]
    public sealed class ValueTupleSerializer<T1, T2> :
        ITypeReader<ValueTuple<T1, T2>, MappingDataNode>,
        ITypeSerializer<ValueTuple<T1, T2>, SequenceDataNode>,
        ITypeCopyCreator<ValueTuple<T1, T2>>
    {
        public (T1, T2) Read(
            ISerializationManager serializationManager,
            MappingDataNode node,
            IDependencyCollection dependencies,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null,
            ISerializationManager.InstantiationDelegate<(T1, T2)>? instanceProvider = null)
        {
            if (node.Children.Count != 1)
                throw new InvalidMappingException("Less than or more than 1 mappings provided to ValueTupleSerializer");

            var entry = node.Children.First();
            var v1 = serializationManager.Read<T1>(node.GetKeyNode(entry.Key), hookCtx, context);
            var v2 = serializationManager.Read<T2>(entry.Value, hookCtx, context);

            return (v1, v2);
        }

        public (T1, T2) Read(
            ISerializationManager serializationManager,
            SequenceDataNode node,
            IDependencyCollection dependencies,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null,
            ISerializationManager.InstantiationDelegate<(T1, T2)>? val = null)
        {
            if (node.Count != 2)
                throw new InvalidMappingException("Sequence must contain exactly 2 elements.");

            var v1 = serializationManager.Read<T1>(node[0], hookCtx, context);
            var v2 = serializationManager.Read<T2>(node[1], hookCtx, context);

            return (v1, v2);
        }

        public ValidationNode Validate(
            ISerializationManager serializationManager,
            MappingDataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context = null)
        {
            if (node.Children.Count != 1)
                return new ErrorNode(node, "More or less than 1 Mapping for ValueTuple found.");

            var entry = node.Children.First();
            var dict = new Dictionary<ValidationNode, ValidationNode>
            {
                {
                    serializationManager.ValidateNode<T1>(node.GetKeyNode(entry.Key), context),
                    serializationManager.ValidateNode<T2>(entry.Value, context)
                }
            };

            return new ValidatedMappingNode(dict);
        }

        public ValidationNode Validate(
            ISerializationManager serializationManager,
            SequenceDataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context = null)
        {
            if (node.Count != 2)
                throw new InvalidMappingException("Sequence must contain exactly 2 elements.");

            var seq = new List<ValidationNode>
            {
                serializationManager.ValidateNode<T1>(node[0], context),
                serializationManager.ValidateNode<T2>(node[1], context)
            };

            return new ValidatedSequenceNode(seq);
        }

        public DataNode Write(
            ISerializationManager serializationManager,
            (T1, T2) value,
            IDependencyCollection dependencies,
            bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new SequenceDataNode(new List<DataNode>
            {
                serializationManager.WriteValue(value.Item1, alwaysWrite, context),
                serializationManager.WriteValue(value.Item2, alwaysWrite, context)
            });
        }

        public (T1, T2) CreateCopy(
            ISerializationManager serializationManager,
            (T1, T2) source,
            IDependencyCollection dependencies,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null)
        {
            return (serializationManager.CreateCopy(source.Item1, hookCtx, context),
                serializationManager.CreateCopy(source.Item2, hookCtx, context));
        }
    }
}
