using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.Manager
{
    public partial class SerializationManager
    {
        private delegate bool WriteDelegate(
            object obj,
            [NotNullWhen(true)] out DataNode? node,
            bool alwaysWrite,
            ISerializationContext? context);

        private readonly Dictionary<Type, object> _typeWriters = new();
        private readonly ConcurrentDictionary<Type, WriteDelegate> _writerDelegates = new();

        private WriteDelegate GetOrCreateWriteDelegate(Type type)
        {
            return _writerDelegates
                .GetOrAdd(type, (_, t) =>
                {
                    var instanceParam = Expression.Constant(this);
                    var objParam = Expression.Parameter(typeof(object), "obj");
                    var nodeParam = Expression.Parameter(typeof(DataNode).MakeByRefType(), "node");
                    var alwaysWriteParam = Expression.Parameter(typeof(bool), "alwaysWrite");
                    var contextParam = Expression.Parameter(typeof(ISerializationContext), "context");

                    var call = Expression.Call(
                        instanceParam,
                        nameof(TryWrite),
                        new[] {t},
                        Expression.Convert(objParam, t),
                        nodeParam,
                        alwaysWriteParam,
                        contextParam);

                    return Expression.Lambda<WriteDelegate>(
                        call,
                        objParam,
                        nodeParam,
                        alwaysWriteParam,
                        contextParam).Compile();
                }, type);
        }

        private bool TryWriteRaw(
            Type type,
            object obj,
            [NotNullWhen(true)] out DataNode? node,
            bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return GetOrCreateWriteDelegate(type)(obj, out node, alwaysWrite, context);
        }

        private bool TryGetWriter<T>(
            ISerializationContext? context,
            [NotNullWhen(true)] out ITypeWriter<T>? writer)
        {
            if (context != null && context.TypeWriters.TryGetValue(typeof(T), out var rawTypeWriter) ||
                _typeWriters.TryGetValue(typeof(T), out rawTypeWriter))
            {
                writer = (ITypeWriter<T>) rawTypeWriter;
                return true;
            }

            return TryGetGenericWriter(out writer);
        }

        private bool TryWrite<T>(
            T obj,
            [NotNullWhen(true)] out DataNode? node,
            bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            node = default;
            if (TryGetWriter<T>(context, out var writer))
            {
                node = writer.Write(this, obj, alwaysWrite, context);
                return true;
            }

            return false;
        }

        private bool TryGetGenericWriter<T>([NotNullWhen(true)] out ITypeWriter<T>? rawWriter)
        {
            rawWriter = null;

            if (typeof(T).IsGenericType)
            {
                var typeDef = typeof(T).GetGenericTypeDefinition();

                Type? serializerTypeDef = null;

                foreach (var (key, val) in _genericWriterTypes)
                {
                    if (typeDef.HasSameMetadataDefinitionAs(key))
                    {
                        serializerTypeDef = val;
                        break;
                    }
                }

                if (serializerTypeDef == null) return false;

                var serializerType = serializerTypeDef.MakeGenericType(typeof(T).GetGenericArguments());
                rawWriter = (ITypeWriter<T>) RegisterSerializer(serializerType)!;

                return true;
            }

            return false;
        }
    }
}
