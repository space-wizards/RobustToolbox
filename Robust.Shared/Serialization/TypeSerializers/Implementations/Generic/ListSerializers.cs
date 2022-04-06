using System.Collections.Generic;
using System.Collections.Immutable;
using JetBrains.Annotations;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations.Generic
{
    [TypeSerializer]
    public sealed class ListSerializers<T> :
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
                sequence.Add(serializationManager.WriteValue(typeof(T), elem, alwaysWrite, context));
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

        List<T> ITypeReader<List<T>, SequenceDataNode>.Read(ISerializationManager serializationManager,
            SequenceDataNode node,
            IDependencyCollection dependencies,
            bool skipHook,
            ISerializationContext? context, List<T>? list)
        {
            list ??= new List<T>();

            foreach (var dataNode in node.Sequence)
            {
                list.Add(serializationManager.Read<T>(dataNode, context, skipHook));
            }

            return list;
        }

        ValidationNode ITypeValidator<ImmutableList<T>, SequenceDataNode>.Validate(
            ISerializationManager serializationManager,
            SequenceDataNode node, IDependencyCollection dependencies, ISerializationContext? context)
        {
            return Validate(serializationManager, node, context);
        }

        ValidationNode ITypeValidator<IReadOnlyCollection<T>, SequenceDataNode>.Validate(
            ISerializationManager serializationManager,
            SequenceDataNode node, IDependencyCollection dependencies, ISerializationContext? context)
        {
            return Validate(serializationManager, node, context);
        }

        ValidationNode ITypeValidator<IReadOnlyList<T>, SequenceDataNode>.Validate(
            ISerializationManager serializationManager,
            SequenceDataNode node, IDependencyCollection dependencies, ISerializationContext? context)
        {
            return Validate(serializationManager, node, context);
        }

        ValidationNode ITypeValidator<List<T>, SequenceDataNode>.Validate(ISerializationManager serializationManager,
            SequenceDataNode node, IDependencyCollection dependencies, ISerializationContext? context)
        {
            return Validate(serializationManager, node, context);
        }

        ValidationNode Validate(ISerializationManager serializationManager, SequenceDataNode sequenceDataNode, ISerializationContext? context)
        {
            var list = new List<ValidationNode>();
            foreach (var elem in sequenceDataNode.Sequence)
            {
                list.Add(serializationManager.ValidateNode(typeof(T), elem, context));
            }

            return new ValidatedSequenceNode(list);
        }

        IReadOnlyList<T> ITypeReader<IReadOnlyList<T>, SequenceDataNode>.Read(
            ISerializationManager serializationManager, SequenceDataNode node,
            IDependencyCollection dependencies,
            bool skipHook, ISerializationContext? context, IReadOnlyList<T>? rawValue)
        {
            if(rawValue != null)
                Logger.Warning($"Provided value to a Read-call for a {nameof(IReadOnlySet<T>)}. Ignoring...");

            var list = new List<T>();

            foreach (var dataNode in node.Sequence)
            {
                list.Add(serializationManager.Read<T>(dataNode, context, skipHook));
            }

            return list;
        }

        IReadOnlyCollection<T> ITypeReader<IReadOnlyCollection<T>, SequenceDataNode>.Read(
            ISerializationManager serializationManager, SequenceDataNode node,
            IDependencyCollection dependencies,
            bool skipHook, ISerializationContext? context, IReadOnlyCollection<T>? rawValue)
        {
            if(rawValue != null)
                Logger.Warning($"Provided value to a Read-call for a {nameof(IReadOnlyCollection<T>)}. Ignoring...");

            var list = new List<T>();

            foreach (var dataNode in node.Sequence)
            {
                list.Add(serializationManager.Read<T>(dataNode, context, skipHook));
            }

            return list;
        }

        ImmutableList<T> ITypeReader<ImmutableList<T>, SequenceDataNode>.Read(
            ISerializationManager serializationManager, SequenceDataNode node,
            IDependencyCollection dependencies,
            bool skipHook, ISerializationContext? context, ImmutableList<T>? rawValue)
        {
            if(rawValue != null)
                Logger.Warning($"Provided value to a Read-call for a {nameof(ImmutableList<T>)}. Ignoring...");

            var list = ImmutableList.CreateBuilder<T>();

            foreach (var dataNode in node.Sequence)
            {
                list.Add(serializationManager.Read<T>(dataNode, context, skipHook));
            }

            return list.ToImmutable();
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
        public List<T> Copy(ISerializationManager serializationManager, List<T> source, List<T> target,
            bool skipHook,
            ISerializationContext? context = null)
        {
            return CopyInternal(serializationManager, source, target, context);
        }

        [MustUseReturnValue]
        public IReadOnlyList<T> Copy(ISerializationManager serializationManager, IReadOnlyList<T> source,
            IReadOnlyList<T> target, bool skipHook, ISerializationContext? context = null)
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
            IReadOnlyCollection<T> target, bool skipHook, ISerializationContext? context = null)
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

        [MustUseReturnValue]
        public ImmutableList<T> Copy(ISerializationManager serializationManager, ImmutableList<T> source,
            ImmutableList<T> target, bool skipHook, ISerializationContext? context = null)
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
