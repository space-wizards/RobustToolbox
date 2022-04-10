using System.Collections.Generic;
using System.Collections.Immutable;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List
{
    public sealed partial class PrototypeIdListSerializer<T> :
        ITypeSerializer<ImmutableList<string>, SequenceDataNode>
        where T : class, IPrototype
    {
        public ValidationNode Validate(ISerializationManager serializationManager, SequenceDataNode node,
            IDependencyCollection dependencies, ISerializationContext? context = null)
        {
            return ValidateInternal(serializationManager, node, dependencies, context);
        }

        public ImmutableList<string> Read(ISerializationManager serializationManager, SequenceDataNode node,
            IDependencyCollection dependencies, bool skipHook, ISerializationContext? context = null,
            ImmutableList<string>? rawValue = null)
        {
            if(rawValue != null)
                Logger.Warning($"Provided value to a Read-call for a {nameof(ImmutableList<string>)}. Ignoring...");

            var builder = ImmutableList.CreateBuilder<string>();

            foreach (var dataNode in node.Sequence)
            {
                builder.Add(_prototypeSerializer.Read(
                    serializationManager,
                    (ValueDataNode) dataNode,
                    dependencies,
                    skipHook,
                    context));
            }

            return builder.ToImmutable();
        }

        public DataNode Write(ISerializationManager serializationManager, ImmutableList<string> value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return WriteInternal(serializationManager, value, alwaysWrite, context);
        }

        public ImmutableList<string> Copy(ISerializationManager serializationManager, ImmutableList<string> source, ImmutableList<string> target,
            bool skipHook, ISerializationContext? context = null)
        {
            return ImmutableList.CreateRange(source);
        }
    }
}
