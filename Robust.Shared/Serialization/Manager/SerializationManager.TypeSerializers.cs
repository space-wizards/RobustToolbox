using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization.Manager
{
    public partial class SerializationManager
    {
        private readonly Dictionary<(Type Type, Type DataNodeType), object> _typeReaders = new();
        private readonly Dictionary<Type, object> _typeWriters = new();
        private readonly Dictionary<Type, object> _typeCopiers = new();
        private readonly Dictionary<(Type Type, Type DataNodeType), object> _typeValidators = new();

        private readonly Dictionary<(Type Type, Type DataNodeType), Type> _genericReaderTypes = new();
        private readonly Dictionary<Type, Type> _genericWriterTypes = new();
        private readonly Dictionary<Type, Type> _genericCopierTypes = new();
        private readonly Dictionary<(Type Type, Type DataNodeType), Type> _genericValidatorTypes = new();

        private readonly Dictionary<Type, object> _customTypeSerializers = new();

        private void InitializeTypeSerializers()
        {
            foreach (var type in _reflectionManager.FindTypesWithAttribute<TypeSerializerAttribute>())
            {
                RegisterSerializer(type);
            }
        }

        private object GetTypeSerializer(Type type)
        {
            if (type.IsGenericTypeDefinition)
                throw new ArgumentException("TypeSerializer cannot be TypeDefinition!", nameof(type));

            if (_customTypeSerializers.TryGetValue(type, out var obj)) return obj;

            obj = Activator.CreateInstance(type)!;
            _customTypeSerializers[type] = obj;
            return obj;
        }

        private object? RegisterSerializer(Type type)
        {
            var writerInterfaces = type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ITypeWriter<>)).ToArray();
            var readerInterfaces = type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ITypeReader<,>)).ToArray();
            var copierInterfaces = type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ITypeCopier<>)).ToArray();
            var validatorInterfaces = type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ITypeValidator<,>)).ToArray();

            if (readerInterfaces.Length == 0 &&
                writerInterfaces.Length == 0 &&
                copierInterfaces.Length == 0 &&
                validatorInterfaces.Length == 0)
            {
                throw new InvalidOperationException(
                    "Tried to register TypeReader/Writer/Copier that had none of the interfaces inherited.");
            }

            if (type.IsGenericTypeDefinition)
            {
                foreach (var writerInterface in writerInterfaces)
                {
                    if (!_genericWriterTypes.TryAdd(writerInterface.GetGenericArguments()[0], type))
                        Logger.ErrorS(LogCategory, $"Tried registering generic writer twice for type {writerInterface.GetGenericArguments()[0]}");
                }

                RegisterGenericReader(type, readerInterfaces);

                foreach (var copierInterface in copierInterfaces)
                {
                    if (!_genericCopierTypes.TryAdd(copierInterface.GetGenericArguments()[0], type))
                        Logger.ErrorS(LogCategory, $"Tried registering generic copier twice for type {copierInterface.GetGenericArguments()[0]}");
                }

                foreach (var validatorInterface in validatorInterfaces)
                {
                    if (!_genericValidatorTypes.TryAdd((validatorInterface.GetGenericArguments()[0], validatorInterface.GetGenericArguments()[1]), type))
                        Logger.ErrorS(LogCategory, $"Tried registering generic reader twice for type {validatorInterface.GetGenericArguments()[0]} and node {validatorInterface.GetGenericArguments()[1]}");
                }

                return null;
            }
            else
            {
                var serializer = GetTypeSerializer(type);

                foreach (var writerInterface in writerInterfaces)
                {
                    if (!_typeWriters.TryAdd(writerInterface.GetGenericArguments()[0], serializer))
                        Logger.ErrorS(LogCategory, $"Tried registering writer twice for type {writerInterface.GetGenericArguments()[0]}");
                }

                RegisterReaders(readerInterfaces, serializer);

                foreach (var copierInterface in copierInterfaces)
                {
                    if (!_typeCopiers.TryAdd(copierInterface.GetGenericArguments()[0], serializer))
                        Logger.ErrorS(LogCategory, $"Tried registering copier twice for type {copierInterface.GetGenericArguments()[0]}");
                }

                foreach (var validatorInterface in validatorInterfaces)
                {
                    if (!_typeValidators.TryAdd((validatorInterface.GetGenericArguments()[0], validatorInterface.GetGenericArguments()[1]), serializer))
                        Logger.ErrorS(LogCategory, $"Tried registering reader twice for type {validatorInterface.GetGenericArguments()[0]} and node {validatorInterface.GetGenericArguments()[1]}");
                }

                return serializer;
            }
        }

        private void RegisterGenericReader(Type type, Type[] readerInterfaces)
        {
            DebugTools.Assert(type.IsGenericTypeDefinition);

            foreach (var readerInterface in readerInterfaces)
            {
                var genericArguments = readerInterface.GetGenericArguments();
                var readType = genericArguments[0];
                var nodeType = genericArguments[1];

                if (!_genericReaderTypes.TryAdd((readType, nodeType), type))
                {
                    Logger.ErrorS(LogCategory,
                        $"Tried registering generic reader twice for type {readType} and node {nodeType}");
                }

                var delegates = _readDelegates.GetOrNew(type);

                if (delegates.TryGetValue(nodeType, out var @delegate))
                {
                    Logger.ErrorS(LogCategory,
                        $"Tried registering generic reader delegate twice for type {readType} and node {nodeType}");
                }

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
                    new[] {type, nodeType},
                    typeParam,
                    Expression.Convert(nodeParam, nodeType),
                    dependenciesParam,
                    objParam,
                    skipHookParam,
                    contextParam);

                @delegate = Expression.Lambda<ReadDelegate>(
                    call,
                    typeParam,
                    nodeParam,
                    dependenciesParam,
                    objParam,
                    skipHookParam,
                    contextParam).Compile();

                delegates[nodeType] = @delegate;
            }
        }

        private void RegisterReaders(Type[] readerInterfaces, object serializer)
        {
            foreach (var readerInterface in readerInterfaces)
            {
                if (!_typeReaders.TryAdd((readerInterface.GetGenericArguments()[0], readerInterface.GetGenericArguments()[1]), serializer))
                    Logger.ErrorS(LogCategory, $"Tried registering reader twice for type {readerInterface.GetGenericArguments()[0]} and node {readerInterface.GetGenericArguments()[1]}");
            }
        }
    }
}
