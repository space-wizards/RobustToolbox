using System;
using Robust.Shared.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;

[TypeSerializer]
public sealed class ObjectSerializer : ITypeSerializer<object, ValueDataNode>, ITypeCopier<object>
{
    #region Validate

    public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        var reflection = dependencies.Resolve<IReflectionManager>();

        if (node.Tag != null)
        {
            string? typeString = node.Tag[6..];

            if (!reflection.TryLooseGetType(typeString, out var type))
            {
                return new ErrorNode(node, $"Unable to find type for {typeString}");
            }

            return serializationManager.ValidateNode(type, node, context);
        }
        return new ErrorNode(node, $"Unable to find type for {node}");
    }

    #endregion

    #region Read
    public object Read(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx, ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<object>? instanceProvider = null)
    {
        var reflection = dependencies.Resolve<IReflectionManager>();
        var value = instanceProvider != null ? instanceProvider() : new object();

        if (node.Tag != null)
        {
            string? typeString = node.Tag[6..];

            if (!reflection.TryLooseGetType(typeString, out var type))
                throw new NullReferenceException($"Found null type for {typeString}");

            value = serializationManager.Read(type, node, hookCtx, context);

            if (value == null)
                throw new NullReferenceException($"Found null data for {node}, expected {type}");
        }

        return value;
    }
    #endregion

    #region Write
    public DataNode Write(ISerializationManager serializationManager, object value,
            IDependencyCollection dependencies, bool alwaysWrite = false,
            ISerializationContext? context = null)
    {
        var node = serializationManager.WriteValue(value.GetType(), value);

        if (node == null)
            throw new NullReferenceException($"Attempted to write node with type {value.GetType()}, node returned null");

        return node;
    }
    #endregion

    #region CopyTo

    public void CopyTo(
        ISerializationManager serializationManager,
        object source,
        ref object target,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null)
    {
        target = source;
    }
    #endregion
}
