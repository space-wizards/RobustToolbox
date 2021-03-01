using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.TypeSerializers.Generic
{
    [TypeSerializer]
    public class HashSetSerializer<T> :
        ITypeSerializer<HashSet<T>, SequenceDataNode>,
        ITypeSerializer<ImmutableHashSet<T>, SequenceDataNode>
    {
        DeserializationResult ITypeReader<HashSet<T>, SequenceDataNode>.Read(ISerializationManager serializationManager,
            SequenceDataNode node,
            ISerializationContext? context)
        {
            var set = new HashSet<T>();
            var mappings = new List<DeserializationResult>();

            foreach (var dataNode in node.Sequence)
            {
                var (value, result) = serializationManager.ReadWithValueOrThrow<T>(dataNode, context);

                set.Add(value);
                mappings.Add(result);
            }

            return new DeserializedMutableCollection<HashSet<T>, T>(set, mappings);
        }

        bool ITypeReader<ImmutableHashSet<T>, SequenceDataNode>.Validate(ISerializationManager serializationManager,
            SequenceDataNode node, ISerializationContext? context = null)
        {
            return Validate(serializationManager, node, context);
        }

        bool ITypeReader<HashSet<T>, SequenceDataNode>.Validate(ISerializationManager serializationManager,
            SequenceDataNode node, ISerializationContext? context = null)
        {
            return Validate(serializationManager, node, context);
        }

        bool Validate(ISerializationManager serializationManager, SequenceDataNode node, ISerializationContext? context)
        {
            foreach (var elem in node.Sequence)
            {
                if (!serializationManager.ValidateNode(typeof(T), elem, context)) return false;
            }

            return true;
        }

        public DataNode Write(ISerializationManager serializationManager, ImmutableHashSet<T> value,
            bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return Write(serializationManager, value.ToHashSet(), alwaysWrite, context);
        }

        public DataNode Write(ISerializationManager serializationManager, HashSet<T> value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            var sequence = new SequenceDataNode();

            foreach (var elem in value)
            {
                sequence.Add(serializationManager.WriteValue(elem, alwaysWrite, context));
            }

            return sequence;
        }

        DeserializationResult ITypeReader<ImmutableHashSet<T>, SequenceDataNode>.Read(
            ISerializationManager serializationManager, SequenceDataNode node,
            ISerializationContext? context)
        {
            var set = ImmutableHashSet.CreateBuilder<T>();
            var mappings = new List<DeserializationResult>();

            foreach (var dataNode in node.Sequence)
            {
                var (value, result) = serializationManager.ReadWithValueOrThrow<T>(dataNode, context);

                set.Add(value);
                mappings.Add(result);
            }

            return new DeserializedImmutableSet<T>(set.ToImmutable(), mappings);
        }

        [MustUseReturnValue]
        public HashSet<T> Copy(ISerializationManager serializationManager, HashSet<T> source, HashSet<T> target, ISerializationContext? context = null)
        {
            target.Clear();
            target.EnsureCapacity(source.Count);

            foreach (var element in source)
            {
                var elementCopy = serializationManager.CreateCopy(element, context) ?? throw new NullReferenceException();
                target.Add(elementCopy);
            }

            return target;
        }

        [MustUseReturnValue]
        public ImmutableHashSet<T> Copy(ISerializationManager serializationManager, ImmutableHashSet<T> source,
            ImmutableHashSet<T> target, ISerializationContext? context = null)
        {
            var builder = ImmutableHashSet.CreateBuilder<T>();

            foreach (var element in source)
            {
                var elementCopy = serializationManager.CreateCopy(element, context) ?? throw new NullReferenceException();
                builder.Add(elementCopy);
            }

            return builder.ToImmutable();
        }
    }
}
