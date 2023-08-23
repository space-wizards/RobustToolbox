using System;
using Robust.Shared.Serialization.Manager;

#pragma warning disable CS0612 // Type or member is obsolete

namespace Robust.Shared.Serialization;

public interface ISerializationGenerated<T> : ISerializationGenerated
{
    [Obsolete("Use ISerializationManager.CopyTo instead")]
    void Copy(
        ref T target,
        ISerializationManager serialization,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null);

    [Obsolete("Use ISerializationManager.CopyTo instead")]
    void InternalCopy(
        ref T target,
        ISerializationManager serialization,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null);

    [Obsolete("Use ISerializationManager.CreateCopy instead")]
    T Instantiate();
}

public interface ISerializationGenerated
{
    [Obsolete("Use ISerializationManager.CopyTo instead")]
    void Copy(
        ref object target,
        ISerializationManager serialization,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null);
}
