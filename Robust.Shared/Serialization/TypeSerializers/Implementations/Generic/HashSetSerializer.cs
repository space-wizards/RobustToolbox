using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
    public sealed class HashSetSerializer<T> :
        ITypeSerializer<HashSet<T>, SequenceDataNode>,
        ITypeSerializer<ImmutableHashSet<T>, SequenceDataNode>,
        ITypeCopier<HashSet<T>>,
        ITypeCopyCreator<ImmutableHashSet<T>>
    {
        HashSet<T> ITypeReader<HashSet<T>, SequenceDataNode>.Read(ISerializationManager serializationManager,
            SequenceDataNode node,
            IDependencyCollection dependencies,
            SerializationHookContext hookCtx,
            ISerializationContext? context,
            ISerializationManager.InstantiationDelegate<HashSet<T>>? instanceProvider)
        {
            var set = instanceProvider != null ? instanceProvider() : new HashSet<T>();

            foreach (var dataNode in node.Sequence)
            {
                set.Add(serializationManager.Read<T>(dataNode, hookCtx, context));
            }

            return set;
        }

        ValidationNode ITypeValidator<ImmutableHashSet<T>, SequenceDataNode>.Validate(
            ISerializationManager serializationManager,
            SequenceDataNode node, IDependencyCollection dependencies, ISerializationContext? context)
        {
            return Validate(serializationManager, node, context);
        }

        ValidationNode ITypeValidator<HashSet<T>, SequenceDataNode>.Validate(ISerializationManager serializationManager,
            SequenceDataNode node, IDependencyCollection dependencies, ISerializationContext? context)
        {
            return Validate(serializationManager, node, context);
        }

        ValidationNode Validate(ISerializationManager serializationManager, SequenceDataNode node,
            ISerializationContext? context)
        {
            var list = new List<ValidationNode>();
            foreach (var elem in node.Sequence)
            {
                list.Add(serializationManager.ValidateNode<T>(elem, context));
            }

            return new ValidatedSequenceNode(list);
        }

        public DataNode Write(ISerializationManager serializationManager, ImmutableHashSet<T> value,
            IDependencyCollection dependencies,
            bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return Write(serializationManager, value.ToHashSet(), dependencies, alwaysWrite, context);
        }

        public DataNode Write(ISerializationManager serializationManager, HashSet<T> value,
            IDependencyCollection dependencies, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            var sequence = new SequenceDataNode();

            foreach (var elem in value)
            {
                sequence.Add(serializationManager.WriteValue(elem, alwaysWrite, context));
            }

            return sequence;
        }

        ImmutableHashSet<T> ITypeReader<ImmutableHashSet<T>, SequenceDataNode>.Read(
            ISerializationManager serializationManager,
            SequenceDataNode node,
            IDependencyCollection dependencies,
            SerializationHookContext hookCtx,
            ISerializationContext? context,
            ISerializationManager.InstantiationDelegate<ImmutableHashSet<T>>? instanceProvider)
        {
            if (instanceProvider != null)
                Logger.Warning($"Provided value to a Read-call for a {nameof(ImmutableHashSet<T>)}. Ignoring...");
            var set = ImmutableHashSet.CreateBuilder<T>();

            foreach (var dataNode in node.Sequence)
            {
                set.Add(serializationManager.Read<T>(dataNode, hookCtx, context));
            }

            return set.ToImmutable();
        }

        public void CopyTo(ISerializationManager serializationManager, HashSet<T> source, ref HashSet<T> target,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null)
        {
            target.Clear();
            target.EnsureCapacity(source.Count);

            foreach (var val in source)
            {
                target.Add(serializationManager.CreateCopy(val, hookCtx, context));
            }
        }

        public ImmutableHashSet<T> CreateCopy(ISerializationManager serializationManager, ImmutableHashSet<T> source,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null)
        {
            var target = new HashSet<T>();
            target.EnsureCapacity(source.Count);

            foreach (var val in source)
            {
                target.Add(serializationManager.CreateCopy(val, hookCtx, context));
            }

            return target.ToImmutableHashSet();
        }
    }
}
