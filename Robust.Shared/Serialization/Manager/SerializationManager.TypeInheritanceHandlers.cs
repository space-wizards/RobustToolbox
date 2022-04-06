using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Robust.Shared.Serialization.Manager;

public partial class SerializationManager
{
    private readonly Dictionary<(Type Type, Type DataNodeType), object> _typeInheritanceHandlers = new();

    private object? GetTypeInheritanceHandler(Type value, Type node)
    {
        if (_typeInheritanceHandlers.TryGetValue((value, node), out var handler))
        {
            return handler;
        }

        if (TryGetGenericInheritanceHandler(value, node, out handler))
        {
            return handler;
        }

        return null;
    }

    private bool TryGetTypeInheritanceHandler(Type value, Type node, [NotNullWhen(true)] out object? handler)
    {
        return (handler = GetTypeInheritanceHandler(value, node)) != null;
    }


    private bool TryGetGenericInheritanceHandler(Type type, Type node, [NotNullWhen(true)] out object? handler)
    {
        if (type.IsGenericType)
        {
            var typeDef = type.GetGenericTypeDefinition();

            Type? serializerTypeDef = null;

            foreach (var (key, val) in _genericInheritanceHandlerTypes)
            {
                if (typeDef.HasSameMetadataDefinitionAs(key.Type) && key.DataNodeType.IsAssignableFrom(node))
                {
                    serializerTypeDef = val;
                    break;
                }
            }

            if (serializerTypeDef == null)
            {
                handler = null;
                return false;
            }

            var serializerType = serializerTypeDef.MakeGenericType(type.GetGenericArguments());

            handler = RegisterSerializer(serializerType) ?? throw new NullReferenceException();
            return true;
        }

        handler = null;
        return false;
    }
}
