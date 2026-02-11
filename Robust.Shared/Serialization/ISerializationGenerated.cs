using System;
using System.Collections.Generic;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Definition;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;

#pragma warning disable CS0612 // Type or member is obsolete

namespace Robust.Shared.Serialization;

public interface ISerializationGenerated<T> : ISerializationGenerated
{
    /// <seealso cref="ISerializationManager.CopyTo"/>
    [Obsolete("Use ISerializationManager.CopyTo instead")]
    void Copy(
        ref T target,
        ISerializationManager serialization,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null);

    /// <seealso cref="ISerializationManager.CopyTo"/>
    [Obsolete("Use ISerializationManager.CopyTo instead")]
    void InternalCopy(
        ref T target,
        ISerializationManager serialization,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null);

    /// <seealso cref="ISerializationManager.CreateCopy"/>
    [Obsolete("Use ISerializationManager.CreateCopy instead")]
    T Instantiate();

    [Obsolete("Use ISerializationManager.ValidateNode instead")]
    static virtual ValidationNode Validate(
        List<ValidationNode> nodes,
        MappingDataNode node,
        ISerializationManager serialization,
        ISerializationContext? context = null)
    {
        throw new NotImplementedException();
    }

    [Obsolete("Use ISerializationManager.ValidateNode instead")]
    static virtual ValidateAllFieldsDelegate RobustValidateDelegate()
    {
        throw new NotImplementedException();
    }
}

public interface ISerializationGenerated
{
    /// <seealso cref="ISerializationManager.CopyTo"/>
    [Obsolete("Use ISerializationManager.CopyTo instead")]
    void Copy(
        ref object target,
        ISerializationManager serialization,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null);
}
