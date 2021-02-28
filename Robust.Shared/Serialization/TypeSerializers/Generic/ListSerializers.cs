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
        private DataNode WriteInternal(ISerializationManager serializationManager, IEnumerable<T> value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            var sequence = new SequenceDataNode();

            foreach (var elem in value)
            {
                sequence.Add(serializationManager.WriteValue(elem, alwaysWrite, context));
            }

            return sequence;
        }

        public DataNode Write(ISerializationManager serializationManager, ImmutableList<T> value,
            bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return WriteInternal(serializationManager, value, alwaysWrite, context);
        }

        public DataNode Write(ISerializationManager serializationManager, List<T> value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return WriteInternal(serializationManager, value, alwaysWrite, context);
        }

        public DataNode Write(ISerializationManager serializationManager, IReadOnlyCollection<T> value,
            bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return WriteInternal(serializationManager, value, alwaysWrite, context);
        }

        public DataNode Write(ISerializationManager serializationManager, IReadOnlyList<T> value,
            bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return WriteInternal(serializationManager, value, alwaysWrite, context);
        }

        DeserializationResult ITypeReader<List<T>, SequenceDataNode>.Read(ISerializationManager serializationManager,
            SequenceDataNode node, ISerializationContext? context)
        {
            var list = new List<T>();
            var results = new List<DeserializationResult>();

            foreach (var dataNode in node.Sequence)
            {
                var (value, result) = serializationManager.ReadWithValueOrThrow<T>(typeof(T), dataNode, context);
                list.Add(value);
                results.Add(result);
            }

            return new DeserializedMutableCollection<List<T>, T>(list, results);
        }

        DeserializationResult ITypeReader<IReadOnlyList<T>, SequenceDataNode>.Read(
            ISerializationManager serializationManager, SequenceDataNode node, ISerializationContext? context)
        {
            var list = new List<T>();
            var results = new List<DeserializationResult>();

            foreach (var dataNode in node.Sequence)
            {
                var (value, result) = serializationManager.ReadWithValueOrThrow<T>(dataNode, context);

                list.Add(value);
                results.Add(result);
            }

            return new DeserializedReadOnlyCollection<IReadOnlyList<T>, T>(list, results, l => l);
        }

        DeserializationResult ITypeReader<IReadOnlyCollection<T>, SequenceDataNode>.Read(
            ISerializationManager serializationManager, SequenceDataNode node, ISerializationContext? context)
        {
            var list = new List<T>();
            var results = new List<DeserializationResult>();

            foreach (var dataNode in node.Sequence)
            {
                var (value, result) = serializationManager.ReadWithValueOrThrow<T>(dataNode, context);
                list.Add(value);
                results.Add(result);
            }

            return new DeserializedReadOnlyCollection<IReadOnlyCollection<T>, T>(list, results, l => l);
        }

        DeserializationResult ITypeReader<ImmutableList<T>, SequenceDataNode>.Read(
            ISerializationManager serializationManager, SequenceDataNode node, ISerializationContext? context)
        {
            var list = ImmutableList.CreateBuilder<T>();
            var results = new List<DeserializationResult>();

            foreach (var dataNode in node.Sequence)
            {
                var (value, result) = serializationManager.ReadWithValueOrThrow<T>(dataNode, context);
                list.Add(value);
                results.Add(result);
            }

            return new DeserializedImmutableList<T>(list.ToImmutable(), results);
        }

        [MustUseReturnValue]
        private TList CopyInternal<TList>(ISerializationManager serializationManager, IEnumerable<T> source, TList target, ISerializationContext? context = null) where TList : IList<T>
        {
            target.Clear();

            foreach (var element in source)
            {
                var elementCopy = serializationManager.CreateCopy(element, context)!;
                target.Add(elementCopy);
            }

            return target;
        }

        [MustUseReturnValue]
        public List<T> Copy(ISerializationManager serializationManager, List<T> source, List<T> target, ISerializationContext? context = null)
        {
            return CopyInternal(serializationManager, source, target, context);
        }

        [MustUseReturnValue]
        public IReadOnlyList<T> Copy(ISerializationManager serializationManager, IReadOnlyList<T> source,
            IReadOnlyList<T> target, ISerializationContext? context = null)
        {
            if (target is List<T> targetList)
            {
                return CopyInternal(serializationManager, source, targetList);
            }

            var list = new List<T>();

            foreach (var element in source)
            {
                var elementCopy = serializationManager.CreateCopy(element, context)!;
                list.Add(elementCopy);
            }

            return list;
        }

        [MustUseReturnValue]
        public IReadOnlyCollection<T> Copy(ISerializationManager serializationManager, IReadOnlyCollection<T> source,
            IReadOnlyCollection<T> target, ISerializationContext? context = null)
        {
            if (target is List<T> targetList)
            {
                return CopyInternal(serializationManager, source, targetList, context);
            }

            var list = new List<T>();

            foreach (var element in source)
            {
                var elementCopy = serializationManager.CreateCopy(element, context)!;
                list.Add(elementCopy);
            }

            return list;
        }

        public ImmutableList<T> Copy(ISerializationManager serializationManager, ImmutableList<T> source,
            ImmutableList<T> target, ISerializationContext? context = null)
        {
            var builder = ImmutableList.CreateBuilder<T>();

            foreach (var element in source)
            {
                var elementCopy = serializationManager.CreateCopy(element, context)!;
                builder.Add(elementCopy);
            }

            return builder.ToImmutable();
        }
    }
}
