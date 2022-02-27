using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Log;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.Manager
{
    public partial class SerializationManager
    {
        private readonly Dictionary<(Type Type, Type DataNodeType), Type> _genericReaderTypes = new();
        private readonly Dictionary<(Type Type, Type DataNodeType), Type> _genericInheritanceHandlerTypes = new();
        private readonly Dictionary<Type, Type> _genericWriterTypes = new();
        private readonly Dictionary<Type, Type> _genericCopierTypes = new();
        private readonly Dictionary<(Type Type, Type DataNodeType), Type> _genericValidatorTypes = new();

        private void InitializeTypeSerializers(IEnumerable<Type> typeSerializers)
        {
            foreach (var type in typeSerializers)
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
            var inheritanceHandlerInterfaces = type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ITypeInheritanceHandler<,>)).ToArray();

            if (readerInterfaces.Length == 0 &&
                writerInterfaces.Length == 0 &&
                copierInterfaces.Length == 0 &&
                validatorInterfaces.Length == 0 &&
                inheritanceHandlerInterfaces.Length == 0)
            {
                throw new InvalidOperationException(
                    "Tried to register TypeReader/Writer/Copier that had none of the interfaces inherited.");
            }

            if (type.IsGenericTypeDefinition)
            {
                foreach (var writerInterface in writerInterfaces)
                {
                    if (!_genericWriterTypes.TryAdd(writerInterface.GetGenericArguments()[0], type))
                        Logger.ErrorS(LogCategory, $"Tried registering generic writer for type {writerInterface.GetGenericArguments()[0]} twice");
                }

                foreach (var readerInterface in readerInterfaces)
                {
                    var genericArguments = readerInterface.GetGenericArguments();
                    var readType = genericArguments[0];
                    var nodeType = genericArguments[1];

                    if (!_genericReaderTypes.TryAdd((readType, nodeType), type))
                        Logger.ErrorS(LogCategory, $"Tried registering generic reader for type {readType} and node {nodeType} twice");
                }

                foreach (var copierInterface in copierInterfaces)
                {
                    if (!_genericCopierTypes.TryAdd(copierInterface.GetGenericArguments()[0], type))
                        Logger.ErrorS(LogCategory, $"Tried registering generic copier for type {copierInterface.GetGenericArguments()[0]} twice");
                }

                foreach (var validatorInterface in validatorInterfaces)
                {
                    if (!_genericValidatorTypes.TryAdd((validatorInterface.GetGenericArguments()[0], validatorInterface.GetGenericArguments()[1]), type))
                        Logger.ErrorS(LogCategory, $"Tried registering generic reader for type {validatorInterface.GetGenericArguments()[0]} and node {validatorInterface.GetGenericArguments()[1]} twice");
                }

                foreach (var inheritanceHandlerInterface in inheritanceHandlerInterfaces)
                {
                    if (!_genericInheritanceHandlerTypes.TryAdd((inheritanceHandlerInterface.GetGenericArguments()[0], inheritanceHandlerInterface.GetGenericArguments()[1]), type))
                        Logger.ErrorS(LogCategory, $"Tried registering generic reader for type {inheritanceHandlerInterface.GetGenericArguments()[0]} and node {inheritanceHandlerInterface.GetGenericArguments()[1]} twice");
                }

                return null;
            }
            else
            {
                var serializer = GetTypeSerializer(type);

                foreach (var writerInterface in writerInterfaces)
                {
                    if (!_typeWriters.TryAdd(writerInterface.GetGenericArguments()[0], serializer))
                        Logger.ErrorS(LogCategory, $"Tried registering writer for type {writerInterface.GetGenericArguments()[0]} twice");
                }

                foreach (var readerInterface in readerInterfaces)
                {
                    if (!_typeReaders.TryAdd((readerInterface.GetGenericArguments()[0], readerInterface.GetGenericArguments()[1]), serializer))
                        Logger.ErrorS(LogCategory, $"Tried registering reader for type {readerInterface.GetGenericArguments()[0]} and node {readerInterface.GetGenericArguments()[1]} twice");
                }

                foreach (var copierInterface in copierInterfaces)
                {
                    if (!_typeCopiers.TryAdd(copierInterface.GetGenericArguments()[0], serializer))
                        Logger.ErrorS(LogCategory, $"Tried registering copier for type {copierInterface.GetGenericArguments()[0]} twice");
                }

                foreach (var validatorInterface in validatorInterfaces)
                {
                    if (!_typeValidators.TryAdd((validatorInterface.GetGenericArguments()[0], validatorInterface.GetGenericArguments()[1]), serializer))
                        Logger.ErrorS(LogCategory, $"Tried registering reader for type {validatorInterface.GetGenericArguments()[0]} and node {validatorInterface.GetGenericArguments()[1]} twice");
                }

                foreach (var inheritanceHandlerInterface in inheritanceHandlerInterfaces)
                {
                    if (!_typeInheritanceHandlers.TryAdd((inheritanceHandlerInterface.GetGenericArguments()[0], inheritanceHandlerInterface.GetGenericArguments()[1]), serializer))
                        Logger.ErrorS(LogCategory, $"Tried registering reader for type {inheritanceHandlerInterface.GetGenericArguments()[0]} and node {inheritanceHandlerInterface.GetGenericArguments()[1]} twice");
                }

                return serializer;
            }
        }
    }
}
