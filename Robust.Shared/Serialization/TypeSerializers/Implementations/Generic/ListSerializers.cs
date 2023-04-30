using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
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
        ITypeSerializer<ImmutableList<T>, SequenceDataNode>,
        ITypeCopier<List<T>>,
        ITypeCopyCreator<IReadOnlyList<T>>,
        ITypeCopyCreator<IReadOnlyCollection<T>>,
        ITypeCopyCreator<ImmutableList<T>>
    {
        private DataNode WriteInternal(ISerializationManager serializationManager, IEnumerable<T> value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            var sequence = new SequenceDataNode();

            foreach (var elem in value)
            {
                sequence.Add(serializationManager.WriteValue<T>(elem, alwaysWrite, context));
            }

            return sequence;
        }

        public DataNode Write(ISerializationManager serializationManager, ImmutableList<T> value,
            IDependencyCollection dependencies,
            bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return WriteInternal(serializationManager, value, alwaysWrite, context);
        }

        public DataNode Write(ISerializationManager serializationManager, List<T> value,
            IDependencyCollection dependencies, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return WriteInternal(serializationManager, value, alwaysWrite, context);
        }

        public DataNode Write(ISerializationManager serializationManager, IReadOnlyCollection<T> value,
            IDependencyCollection dependencies,
            bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return WriteInternal(serializationManager, value, alwaysWrite, context);
        }

        public DataNode Write(ISerializationManager serializationManager, IReadOnlyList<T> value,
            IDependencyCollection dependencies,
            bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return WriteInternal(serializationManager, value, alwaysWrite, context);
        }

        List<T> ITypeReader<List<T>, SequenceDataNode>.Read(ISerializationManager serializationManager,
            SequenceDataNode node,
            IDependencyCollection dependencies,
            SerializationHookContext hookCtx,
            ISerializationContext? context, ISerializationManager.InstantiationDelegate<List<T>>? instanceProvider)
        {
            var list = instanceProvider != null ? instanceProvider() : new List<T>();

            foreach (var dataNode in node.Sequence)
            {
                list.Add(serializationManager.Read<T>(dataNode, hookCtx, context));
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
                list.Add(serializationManager.ValidateNode<T>(elem, context));
            }

            return new ValidatedSequenceNode(list);
        }

        IReadOnlyList<T> ITypeReader<IReadOnlyList<T>, SequenceDataNode>.Read(
            ISerializationManager serializationManager, SequenceDataNode node,
            IDependencyCollection dependencies,
            SerializationHookContext hookCtx, ISerializationContext? context,
            ISerializationManager.InstantiationDelegate<IReadOnlyList<T>>? instanceProvider)
        {
            if(instanceProvider != null)
                Logger.Warning($"Provided value to a Read-call for a {nameof(IReadOnlySet<T>)}. Ignoring...");

            var list = new List<T>();

            foreach (var dataNode in node.Sequence)
            {
                list.Add(serializationManager.Read<T>(dataNode, hookCtx, context));
            }

            return list;
        }

        IReadOnlyCollection<T> ITypeReader<IReadOnlyCollection<T>, SequenceDataNode>.Read(
            ISerializationManager serializationManager, SequenceDataNode node,
            IDependencyCollection dependencies,
            SerializationHookContext hookCtx, ISerializationContext? context,
            ISerializationManager.InstantiationDelegate<IReadOnlyCollection<T>>? instanceProvider)
        {
            if(instanceProvider != null)
                Logger.Warning($"Provided value to a Read-call for a {nameof(IReadOnlyCollection<T>)}. Ignoring...");

            var list = new List<T>();

            foreach (var dataNode in node.Sequence)
            {
                list.Add(serializationManager.Read<T>(dataNode, hookCtx, context));
            }

            return list;
        }

        ImmutableList<T> ITypeReader<ImmutableList<T>, SequenceDataNode>.Read(
            ISerializationManager serializationManager, SequenceDataNode node,
            IDependencyCollection dependencies,
            SerializationHookContext hookCtx, ISerializationContext? context,
            ISerializationManager.InstantiationDelegate<ImmutableList<T>>? instanceProvider)
        {
            if(instanceProvider != null)
                Logger.Warning($"Provided value to a Read-call for a {nameof(ImmutableList<T>)}. Ignoring...");

            var list = ImmutableList.CreateBuilder<T>();

            foreach (var dataNode in node.Sequence)
            {
                list.Add(serializationManager.Read<T>(dataNode, hookCtx, context));
            }

            return list.ToImmutable();
        }

        public void CopyTo(ISerializationManager serializationManager, List<T> source, ref List<T> target,
            IDependencyCollection dependencies,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null)
        {
            target.Clear();
            target.EnsureCapacity(source.Count);

            var sourceSpan = CollectionsMarshal.AsSpan(source);
            for (var i = 0; i < sourceSpan.Length; i++)
            {
                ref var val = ref sourceSpan[i];
                target.Add(serializationManager.CreateCopy(val, hookCtx, context));
            }
        }

        public IReadOnlyList<T> CreateCopy(ISerializationManager serializationManager, IReadOnlyList<T> source,
            IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null)
        {
            var target = new List<T>(source.Count);

            foreach (var val in source)
            {
                target.Add(serializationManager.CreateCopy(val, hookCtx, context));
            }

            return target;
        }

        public IReadOnlyCollection<T> CreateCopy(ISerializationManager serializationManager, IReadOnlyCollection<T> source,
            IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null)
        {
            var target = new List<T>(source.Count);

            foreach (var val in source)
            {
                target.Add(serializationManager.CreateCopy(val, hookCtx, context));
            }

            return target;
        }

        public ImmutableList<T> CreateCopy(ISerializationManager serializationManager, ImmutableList<T> source,
            IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null)
        {
            var target = new List<T>(source.Count);

            foreach (var val in source)
            {
                target.Add(serializationManager.CreateCopy(val, hookCtx, context));
            }

            return target.ToImmutableList();
        }
    }
}
