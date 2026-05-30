using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;

namespace Robust.Shared.GameObjects;

/// <summary>
/// Generated fast path for copying already-deserialized prototype component data into runtime components.
/// </summary>
public interface IComponentPrototypeCopy
{
    void CopyPrototypeTo(
        ref IComponent target,
        ISerializationManager serialization,
        SerializationHookContext hookCtx,
        ISerializationContext? context);
}

public interface IComponentPrototypeCopy<T>
    where T : IComponent
{
    void CopyPrototypeTo(
        ref T target,
        ISerializationManager serialization,
        SerializationHookContext hookCtx,
        ISerializationContext? context);
}
