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
        [Dependency] private readonly ISerializationManager _serializationManager = default!;

        DeserializationResult<HashSet<T>> ITypeReader<HashSet<T>, SequenceDataNode>.Read(SequenceDataNode node,
            ISerializationContext? context)
        {
            var set = new HashSet<T>();
            var mappings = new List<DeserializationResult>();

            foreach (var dataNode in node.Sequence)
            {
                var result = _serializationManager.ReadOrThrow<T>(dataNode, context);

                set.Add(result.ValueOrThrow);
                mappings.Add(result);
            }

            return new DeserializedCollection<HashSet<T>>(set, mappings);
        }

        public DataNode Write(ImmutableHashSet<T> value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return Write(value.ToHashSet(), alwaysWrite, context);
        }

        public DataNode Write(HashSet<T> value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            var sequence = new SequenceDataNode();

            foreach (var elem in value)
            {
                sequence.Add(_serializationManager.WriteValue(elem, alwaysWrite, context));
            }

            return sequence;
        }

        DeserializationResult<ImmutableHashSet<T>> ITypeReader<ImmutableHashSet<T>, SequenceDataNode>.Read(SequenceDataNode node,
            ISerializationContext? context)
        {
            var set = ImmutableHashSet.CreateBuilder<T>();
            var mappings = new List<DeserializationResult>();

            foreach (var dataNode in node.Sequence)
            {
                var result = _serializationManager.ReadOrThrow<T>(dataNode, context);

                set.Add(result.ValueOrThrow);
                mappings.Add(result);
            }

            return new DeserializedCollection<ImmutableHashSet<T>>(set.ToImmutable(), mappings);
        }

        [MustUseReturnValue]
        public HashSet<T> Copy(HashSet<T> source, HashSet<T> target)
        {
            target.Clear();
            target.EnsureCapacity(source.Count);

            foreach (var element in source)
            {
                var elementCopy = _serializationManager.CreateCopy(element) ?? throw new NullReferenceException();
                target.Add(elementCopy);
            }

            return target;
        }

        [MustUseReturnValue]
        public ImmutableHashSet<T> Copy(ImmutableHashSet<T> source, ImmutableHashSet<T> target)
        {
            var builder = ImmutableHashSet.CreateBuilder<T>();

            foreach (var element in source)
            {
                var elementCopy = _serializationManager.CreateCopy(element) ?? throw new NullReferenceException();
                builder.Add(elementCopy);
            }

            return builder.ToImmutable();
        }
    }
}
