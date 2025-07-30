using Robust.Shared.EntitySerialization;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using static Robust.Shared.Serialization.Manager.ISerializationManager;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;

// This specifically implements WeakEntityReference<T>, but not WeakEntityReference for the same reason that there is no
// EntityUid serializer: So that it can be implemented by the entity (de)serialization context.
// Ideally I'd also leave that there instead of here, but it needs generics...
[TypeSerializer]
public sealed class WeakEntityReferenceSerializer<T> :
    ITypeSerializer<WeakEntityReference<T>, ValueDataNode>
    where T : class, IComponent
{
    public WeakEntityReference<T> Read(
        ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        InstantiationDelegate<WeakEntityReference<T>>? instanceProvider = null)
    {
        var val = serializationManager.Read<WeakEntityReference>(node, hookCtx, context);
        return new(val.Entity);
    }

    public DataNode Write(
        ISerializationManager serializationManager,
        WeakEntityReference<T> value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        EntityUid val = value.Entity;

        if (context is EntitySerializer seri)
        {
            if (!seri.EntMan.EntityExists(val) || !seri.EntMan.HasComponent<T>(val))
                val = EntityUid.Invalid;
        }

        return serializationManager.WriteValue(new WeakEntityReference(val), alwaysWrite, context);
    }

    public ValidationNode Validate(
        ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context)
    {
        return serializationManager.ValidateNode<WeakEntityReference>(node, context);
    }
}
