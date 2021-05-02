using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.Manager
{
    public partial class SerializationManager
    {
        public delegate bool ReadDelegate(
            Type type,
            DataNode node,
            IDependencyCollection dependencies,
            [NotNullWhen(true)] out DeserializationResult? obj,
            bool skipHook,
            ISerializationContext? context = null);

        private readonly Dictionary<(Type Type, Type DataNodeType), object> _typeReaders = new();
        private readonly ConcurrentDictionary<Type, ConcurrentDictionary<Type, ReadDelegate>> _readDelegates = new();

        private ReadDelegate GetOrCreateReadDelegate(Type type, Type nodeType)
        {
            return _readDelegates
                .GetOrAdd(type, _ => new ConcurrentDictionary<Type, ReadDelegate>())
                .GetOrAdd(nodeType, (nT, t) =>
                {
                    var instanceParam = Expression.Constant(this);
                    var typeParam = Expression.Parameter(typeof(Type), "type");
                    var nodeParam = Expression.Parameter(typeof(DataNode), "node");
                    var dependenciesParam = Expression.Parameter(typeof(IDependencyCollection), "dependencies");
                    var objParam = Expression.Parameter(typeof(DeserializationResult).MakeByRefType(), "obj");
                    var skipHookParam = Expression.Parameter(typeof(bool), "skipHook");
                    var contextParam = Expression.Parameter(typeof(ISerializationContext), "context");

                    var call = Expression.Call(
                        instanceParam,
                        nameof(TryRead),
                        new[] {t, nT},
                        typeParam,
                        Expression.Convert(nodeParam, nT),
                        dependenciesParam,
                        objParam,
                        skipHookParam,
                        contextParam);

                    return Expression.Lambda<ReadDelegate>(
                        call,
                        typeParam,
                        nodeParam,
                        dependenciesParam,
                        objParam,
                        skipHookParam,
                        contextParam).Compile();
                }, type);
        }

        private bool TryGetReader<T, TNode>(
            Type type,
            ISerializationContext? context,
            [NotNullWhen(true)] out ITypeReader<T, TNode>? reader)
            where T : notnull
            where TNode : DataNode
        {
            var nodeType = typeof(TNode);

            if (context != null &&
                context.TypeReaders.TryGetValue((type, nodeType), out var rawTypeReader) ||
                _typeReaders.TryGetValue((type, nodeType), out rawTypeReader))
            {
                reader = (ITypeReader<T, TNode>) rawTypeReader;
                return true;
            }

            return TryGetGenericReader(out reader);
        }

        private bool TryRead<T, TNode>(
            Type type,
            TNode node,
            IDependencyCollection dependencies,
            [NotNullWhen(true)] out DeserializationResult? obj,
            bool skipHook,
            ISerializationContext? context = null)
            where T : notnull
            where TNode : DataNode
        {
            if (TryGetReader<T, TNode>(type, context, out var reader))
            {
                obj = reader.Read(this, node, dependencies, skipHook, context);
                return true;
            }

            obj = null;
            return false;
        }

        private bool TryReadRaw(
            Type type,
            DataNode node,
            IDependencyCollection dependencies,
            [NotNullWhen(true)] out DeserializationResult? obj,
            bool skipHook,
            ISerializationContext? context = null)
        {
            return GetOrCreateReadDelegate(type, node.GetType())(type, node, dependencies, out obj, skipHook, context);
        }

        private bool TryGetGenericReader<T, TNode>(
            [NotNullWhen(true)] out ITypeReader<T, TNode>? reader)
            where TNode : DataNode
            where T : notnull
        {
            var type = typeof(T);
            var nodeType = typeof(TNode);

            if (type.IsGenericType)
            {
                var typeDef = type.GetGenericTypeDefinition();

                Type? serializerTypeDef = null;

                foreach (var (key, val) in _genericReaderTypes)
                {
                    if (typeDef.HasSameMetadataDefinitionAs(key.Type) && key.DataNodeType.IsAssignableFrom(nodeType))
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

                reader = (ITypeReader<T, TNode>) (RegisterSerializer(serializerType) ??
                                                  throw new NullReferenceException());
                return true;
            }

            reader = null;
            return false;
        }

    }
}
