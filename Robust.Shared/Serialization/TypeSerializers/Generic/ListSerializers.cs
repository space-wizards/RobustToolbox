using System.Collections.Generic;
using System.Collections.Immutable;
using JetBrains.Annotations;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.TypeSerializers.Generic
{
    [TypeSerializer]
    public class ListSerializers<T> :
        ITypeSerializer<List<T>, SequenceDataNode>,
        ITypeSerializer<IReadOnlyList<T>, SequenceDataNode>,
        ITypeSerializer<IReadOnlyCollection<T>, SequenceDataNode>,
        ITypeSerializer<ImmutableList<T>, SequenceDataNode>
    {
        [Dependency] private readonly ISerializationManager _serializationManager = default!;

        public DeserializationResult<TReturn> Read<TList, TReturn>(
            SequenceDataNode node,
            ISerializationContext? context = null)
            where TList : IList<T>, TReturn, new()
            where TReturn : IEnumerable<T>
        {
            var list = new TList();
            var results = new List<DeserializationResult>();

            foreach (var dataNode in node.Sequence)
            {
                var result = _serializationManager.Read<T>(dataNode, context);
                list.Add(result.ValueOrThrow);
                results.Add(result);
            }

            return new DeserializedCollection<TReturn>(list, results);
        }

        private DataNode WriteInternal(IEnumerable<T> value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            var sequence = new SequenceDataNode();

            foreach (var elem in value)
            {
                sequence.Add(_serializationManager.WriteValue(elem, alwaysWrite, context));
            }

            return sequence;
        }

        public DataNode Write(ImmutableList<T> value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return WriteInternal(value, alwaysWrite, context);
        }

        public DataNode Write(List<T> value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return WriteInternal(value, alwaysWrite, context);
        }

        public DataNode Write(IReadOnlyCollection<T> value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return WriteInternal(value, alwaysWrite, context);
        }

        public DataNode Write(IReadOnlyList<T> value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return WriteInternal(value, alwaysWrite, context);
        }

        DeserializationResult<List<T>> ITypeReader<List<T>, SequenceDataNode>.Read(SequenceDataNode node, ISerializationContext? context)
        {
            return Read<List<T>, List<T>>(node, context);
        }

        DeserializationResult<IReadOnlyList<T>> ITypeReader<IReadOnlyList<T>, SequenceDataNode>.Read(SequenceDataNode node, ISerializationContext? context)
        {
            return Read<List<T>, IReadOnlyList<T>>(node, context);
        }

        DeserializationResult<IReadOnlyCollection<T>> ITypeReader<IReadOnlyCollection<T>, SequenceDataNode>.Read(SequenceDataNode node, ISerializationContext? context)
        {
            return Read<List<T>, IReadOnlyCollection<T>>(node, context);
        }

        DeserializationResult<ImmutableList<T>> ITypeReader<ImmutableList<T>, SequenceDataNode>.Read(SequenceDataNode node, ISerializationContext? context)
        {
            var list = ImmutableList.CreateBuilder<T>();
            var results = new List<DeserializationResult>();

            foreach (var dataNode in node.Sequence)
            {
                var result = _serializationManager.Read<T>(dataNode, context);
                list.Add(result.ValueOrThrow);
                results.Add(result);
            }

            return new DeserializedCollection<ImmutableList<T>>(list.ToImmutable(), results);
        }

        [MustUseReturnValue]
        private TList CopyInternal<TList>(IEnumerable<T> source, TList target) where TList : IList<T>
        {
            target.Clear();

            foreach (var element in source)
            {
                var elementCopy = _serializationManager.CreateCopy(element)!;
                target.Add(elementCopy);
            }

            return target;
        }

        [MustUseReturnValue]
        public List<T> Copy(List<T> source, List<T> target)
        {
            return CopyInternal(source, target);
        }

        [MustUseReturnValue]
        public IReadOnlyList<T> Copy(IReadOnlyList<T> source, IReadOnlyList<T> target)
        {
            if (target is List<T> targetList)
            {
                return CopyInternal(source, targetList);
            }

            var list = new List<T>();

            foreach (var element in source)
            {
                var elementCopy = _serializationManager.CreateCopy(element)!;
                list.Add(elementCopy);
            }

            return list;
        }

        [MustUseReturnValue]
        public IReadOnlyCollection<T> Copy(IReadOnlyCollection<T> source, IReadOnlyCollection<T> target)
        {
            if (target is List<T> targetList)
            {
                return CopyInternal(source, targetList);
            }

            var list = new List<T>();

            foreach (var element in source)
            {
                var elementCopy = _serializationManager.CreateCopy(element)!;
                list.Add(elementCopy);
            }

            return list;
        }

        public ImmutableList<T> Copy(ImmutableList<T> source, ImmutableList<T> target)
        {
            var builder = ImmutableList.CreateBuilder<T>();

            foreach (var element in source)
            {
                var elementCopy = _serializationManager.CreateCopy(element)!;
                builder.Add(elementCopy);
            }

            return builder.ToImmutable();
        }
    }
}
