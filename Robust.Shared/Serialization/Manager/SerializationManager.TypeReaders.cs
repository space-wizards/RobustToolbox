using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization.Manager
{
    public partial class SerializationManager
    {
        private readonly Dictionary<(Type Type, Type DataNodeType), object?> _typeReaders = new();

        private object? GetTypeReader(Type value, Type node)
        {
            using (_serializerLock.ReadGuard())
            {
                if (_typeReaders.TryGetValue((value, node), out var reader))
                    return reader;
            }

            using (_serializerLock.WriteGuard())
            {
                // Check again, in case it got added after releasing the read lock.
                if (_typeReaders.TryGetValue((value, node), out var reader))
                    return reader;

                if (TryGetGenericReader(value, node, out reader))
                    return reader;

                _typeReaders.Add((value, node), null);
                return null;
            }
        }

        private bool TryGetTypeReader(Type value, Type node, [NotNullWhen(true)] out object? reader)
        {
            return (reader = GetTypeReader(value, node)) != null;
        }

        private bool TryGetGenericReader(Type type, Type node, [NotNullWhen(true)] out object? reader)
        {
            if (type.IsGenericType)
            {
                var typeDef = type.GetGenericTypeDefinition();

                Type? serializerTypeDef = null;

                foreach (var (key, val) in _genericReaderTypes)
                {
                    if (typeDef.HasSameMetadataDefinitionAs(key.Type) && key.DataNodeType.IsAssignableFrom(node))
                    {
                        serializerTypeDef = val;
                        break;
                    }
                }

                if (serializerTypeDef == null)
                {
                    reader = null;
                    return false;
                }

                var serializerType = serializerTypeDef.MakeGenericType(type.GetGenericArguments());

                reader = RegisterSerializer(serializerType) ?? throw new NullReferenceException();
                return true;
            }

            reader = null;
            return false;
        }
    }
}
