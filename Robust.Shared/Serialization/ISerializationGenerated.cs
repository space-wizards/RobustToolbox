using Robust.Shared.Serialization.Manager;

#pragma warning disable CS0612 // Type or member is obsolete

namespace Robust.Shared.Serialization;

public interface ISerializationGenerated<T> : ISerializationGenerated
{
    void Copy(
        ref T target,
        ISerializationManager serialization,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null);

    void InternalCopy(
        ref T target,
        ISerializationManager serialization,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null);

    T Instantiate();
}

public interface ISerializationGenerated
{
    void Copy(
        ref object target,
        ISerializationManager serialization,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null);
}
