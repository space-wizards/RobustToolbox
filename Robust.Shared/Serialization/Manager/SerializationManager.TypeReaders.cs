using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.Manager
{
    public partial class SerializationManager
    {
        private readonly Dictionary<(Type Type, Type DataNodeType), object> _typeReaders = new();

        private object? GetTypeReader(Type value, Type node)
        {
            if (_typeReaders.TryGetValue((value, node), out var reader))
            {
                return reader;
            }

            if (TryGetGenericReader(value, node, out reader))
            {
                return reader;
            }

            return null;
        }

        private bool TryGetTypeReader(Type value, Type node, [NotNullWhen(true)] out object? reader)
        {
            return (reader = GetTypeReader(value, node)) != null;
        }

        private bool TryGetTypeReader<TValue, TNode>(
            Type type,
            ISerializationContext? context,
            [NotNullWhen(true)] out ITypeReader<TValue, TNode>? reader)
            where TValue : notnull
            where TNode : DataNode
        {
            var nodeType = typeof(TNode);

            if (context != null &&
                context.TypeReaders.TryGetValue((type, nodeType), out var rawTypeReader) ||
                _typeReaders.TryGetValue((type, nodeType), out rawTypeReader))
            {
                reader = (ITypeReader<TValue, TNode>) rawTypeReader;
                return true;
            }

            return TryGetGenericReader(out reader);
        }

        private bool TryRead<TValue, TNode>(
            Type type,
            TNode node,
            IDependencyCollection dependencies,
            [NotNullWhen(true)] out DeserializationResult? obj,
            bool skipHook,
            ISerializationContext? context = null)
            where TValue : notnull
            where TNode : DataNode
        {
            if (TryGetTypeReader<TValue, TNode>(type, context, out var reader))
            {
                obj = reader.Read(this, node, dependencies, skipHook, context);
                return true;
            }

            obj = null;
            return false;
        }

        private bool TryGetGenericReader<T, TNode>(
            [NotNullWhen(true)] out ITypeReader<T, TNode>? reader)
            where TNode : DataNode
            where T : notnull
        {
            if (TryGetGenericReader(typeof(T), typeof(TNode), out var readerUnCast))
            {
                reader = (ITypeReader<T, TNode>) readerUnCast;
                return true;
            }

            reader = null;
            return false;
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
