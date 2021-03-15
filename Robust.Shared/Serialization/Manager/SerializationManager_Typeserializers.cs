using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

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

            if (readerInterfaces.Length == 0 && writerInterfaces.Length == 0 && copierInterfaces.Length == 0 && validatorInterfaces.Length == 0)
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
                    if (!_genericReaderTypes.TryAdd((readerInterface.GetGenericArguments()[0], readerInterface.GetGenericArguments()[1]), type))
                        Logger.ErrorS(LogCategory, $"Tried registering generic reader for type {readerInterface.GetGenericArguments()[0]} and datanode {readerInterface.GetGenericArguments()[1]} twice");
                }

                foreach (var copierInterface in copierInterfaces)
                {
                    if (!_genericCopierTypes.TryAdd(copierInterface.GetGenericArguments()[0], type))
                        Logger.ErrorS(LogCategory, $"Tried registering generic copier for type {copierInterface.GetGenericArguments()[0]} twice");
                }

                foreach (var validatorInterface in validatorInterfaces)
                {
                    if (!_genericValidatorTypes.TryAdd((validatorInterface.GetGenericArguments()[0], validatorInterface.GetGenericArguments()[1]), type))
                        Logger.ErrorS(LogCategory, $"Tried registering generic reader for type {validatorInterface.GetGenericArguments()[0]} and datanode {validatorInterface.GetGenericArguments()[1]} twice");
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
                        Logger.ErrorS(LogCategory, $"Tried registering reader for type {readerInterface.GetGenericArguments()[0]} and datanode {readerInterface.GetGenericArguments()[1]} twice");
                }

                foreach (var copierInterface in copierInterfaces)
                {
                    if (!_typeCopiers.TryAdd(copierInterface.GetGenericArguments()[0], serializer))
                        Logger.ErrorS(LogCategory, $"Tried registering copier for type {copierInterface.GetGenericArguments()[0]} twice");
                }

                foreach (var validatorInterface in validatorInterfaces)
                {
                    if (!_typeValidators.TryAdd((validatorInterface.GetGenericArguments()[0], validatorInterface.GetGenericArguments()[1]), serializer))
                        Logger.ErrorS(LogCategory, $"Tried registering reader for type {validatorInterface.GetGenericArguments()[0]} and datanode {validatorInterface.GetGenericArguments()[1]} twice");
                }

                return serializer;
            }
        }

        #region TryGetGeneric
        private bool TryGetGenericReader<T, TNode>([NotNullWhen(true)] out ITypeReader<T, TNode>? rawReader)
            where TNode : DataNode where T : notnull
        {
            rawReader = null;

            if (typeof(T).IsGenericType)
            {
                var typeDef = typeof(T).GetGenericTypeDefinition();

                Type? serializerTypeDef = null;

                foreach (var (key, val) in _genericReaderTypes)
                {
                    if (typeDef.HasSameMetadataDefinitionAs(key.Type) && key.DataNodeType.IsAssignableFrom(typeof(TNode)))
                    {
                        serializerTypeDef = val;
                        break;
                    }
                }

                if (serializerTypeDef == null) return false;

                var serializerType = serializerTypeDef.MakeGenericType(typeof(T).GetGenericArguments());
                rawReader = (ITypeReader<T, TNode>) RegisterSerializer(serializerType)!;

                return true;
            }

            return false;
        }

        private bool TryGetGenericWriter<T>([NotNullWhen(true)] out ITypeWriter<T>? rawWriter) where T : notnull
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

        private bool TryGetGenericCopier<T>([NotNullWhen(true)] out ITypeCopier<T>? rawCopier) where T : notnull
        {
            rawCopier = null;

            if (typeof(T).IsGenericType)
            {
                var typeDef = typeof(T).GetGenericTypeDefinition();

                Type? serializerTypeDef = null;

                foreach (var (key, val) in _genericCopierTypes)
                {
                    if (typeDef.HasSameMetadataDefinitionAs(key))
                    {
                        serializerTypeDef = val;
                        break;
                    }
                }

                if (serializerTypeDef == null) return false;

                var serializerType = serializerTypeDef.MakeGenericType(typeof(T).GetGenericArguments());
                rawCopier = (ITypeCopier<T>) RegisterSerializer(serializerType)!;

                return true;
            }

            return false;
        }

        private bool TryGetGenericValidator<T, TNode>([NotNullWhen(true)] out ITypeValidator<T, TNode>? rawReader)
            where TNode : DataNode where T : notnull
        {
            rawReader = null;

            if (typeof(T).IsGenericType)
            {
                var typeDef = typeof(T).GetGenericTypeDefinition();

                Type? serializerTypeDef = null;

                foreach (var (key, val) in _genericValidatorTypes)
                {
                    if (typeDef.HasSameMetadataDefinitionAs(key.Type) && key.DataNodeType.IsAssignableFrom(typeof(TNode)))
                    {
                        serializerTypeDef = val;
                        break;
                    }
                }

                if (serializerTypeDef == null) return false;

                var serializerType = serializerTypeDef.MakeGenericType(typeof(T).GetGenericArguments());
                rawReader = (ITypeValidator<T, TNode>) RegisterSerializer(serializerType)!;

                return true;
            }

            return false;
        }
        #endregion

        #region TryValidate
        private bool TryValidateWithTypeValidator(
            Type type,
            DataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context,
            [NotNullWhen(true)] out ValidationNode? valid)
        {
            //TODO Paul: do this shit w/ delegates
            var method = typeof(SerializationManager).GetRuntimeMethods().First(m =>
                m.Name == nameof(TryValidateWithTypeValidator) && m.GetParameters().Length == 4).MakeGenericMethod(type, node.GetType());

            var arr = new object?[] {node, dependencies, context, null};
            var res = method.Invoke(this, arr);

            if (res as bool? ?? false)
            {
                valid = (ValidationNode) arr[3]!;
                return true;
            }

            valid = null;
            return false;
        }

        private bool TryValidateWithTypeValidator<T, TNode>(
            TNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context,
            [NotNullWhen(true)] out ValidationNode? valid)
            where T : notnull
            where TNode : DataNode
        {
            if (TryGetValidator<T, TNode>(null, out var reader))
            {
                valid = reader.Validate(this, node, dependencies, context);
                return true;
            }

            valid = null;
            return false;
        }

        private bool TryGetValidator<T, TNode>(
            ISerializationContext? context,
            [NotNullWhen(true)] out ITypeValidator<T, TNode>? reader)
            where T : notnull
            where TNode : DataNode
        {
            if (context != null && context.TypeValidators.TryGetValue((typeof(T), typeof(TNode)), out var rawTypeValidator) ||
                _typeValidators.TryGetValue((typeof(T), typeof(TNode)), out rawTypeValidator))
            {
                reader = (ITypeReader<T, TNode>) rawTypeValidator;
                return true;
            }

            return TryGetGenericValidator(out reader);
        }
        #endregion

        #region TryRead
        private bool TryGetReader<T, TNode>(
            ISerializationContext? context,
            [NotNullWhen(true)] out ITypeReader<T, TNode>? reader)
            where T : notnull
            where TNode : DataNode
        {
            if (context != null && context.TypeReaders.TryGetValue((typeof(T), typeof(TNode)), out var rawTypeReader) ||
                _typeReaders.TryGetValue((typeof(T), typeof(TNode)), out rawTypeReader))
            {
                reader = (ITypeReader<T, TNode>) rawTypeReader;
                return true;
            }

            return TryGetGenericReader(out reader);
        }

        private bool TryReadWithTypeSerializers(
            Type type,
            DataNode node,
            IDependencyCollection dependencies,
            [NotNullWhen(true)] out DeserializationResult? obj,
            bool skipHook,
            ISerializationContext? context = null)
        {
            //TODO Paul: do this shit w/ delegates
            var method = typeof(SerializationManager).GetRuntimeMethods()
                .First(m => m.Name == nameof(TryReadWithTypeSerializers) && m.GetParameters().Length == 5)
                .MakeGenericMethod(type, node.GetType());

            obj = default;

            var arr = new object?[] {node, dependencies, obj, skipHook, context};
            var res = method.Invoke(this, arr);

            if (res as bool? ?? false)
            {
                obj = (DeserializationResult) arr[2]!;
                return true;
            }

            return false;
        }

        private bool TryReadWithTypeSerializers<T, TNode>(
            TNode node,
            IDependencyCollection dependencies,
            [NotNullWhen(true)] out DeserializationResult? obj,
            bool skipHook,
            ISerializationContext? context = null)
            where T : notnull
            where TNode : DataNode
        {
            if (TryGetReader<T, TNode>(context, out var reader))
            {
                obj = reader.Read(this, node, dependencies, skipHook, context);
                return true;
            }

            obj = null;
            return false;
        }
        #endregion

        #region TryWrite
        private bool TryWriteWithTypeSerializers(
            Type type,
            object obj,
            [NotNullWhen(true)] out DataNode? node,
            bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            //TODO Paul: do this shit w/ delegates
            var method = typeof(SerializationManager).GetRuntimeMethods().First(m =>
                m.Name == nameof(TryWriteWithTypeSerializers) && m.GetParameters().Length == 4).MakeGenericMethod(type);

            node = null;

            var arr = new[] {obj, node, alwaysWrite, context};
            var res = method.Invoke(this, arr);

            if (res as bool? ?? false)
            {
                node = (DataNode) arr[1]!;
                return true;
            }

            return false;
        }

        private bool TryGetWriter<T>(ISerializationContext? context, [NotNullWhen(true)] out ITypeWriter<T>? writer) where T : notnull
        {
            if (context != null && context.TypeWriters.TryGetValue(typeof(T), out var rawTypeWriter) ||
                _typeWriters.TryGetValue(typeof(T), out rawTypeWriter))
            {
                writer = (ITypeWriter<T>) rawTypeWriter;
                return true;
            }

            return TryGetGenericWriter(out writer);
        }

        private bool TryWriteWithTypeSerializers<T>(
            T obj,
            [NotNullWhen(true)] out DataNode? node,
            bool alwaysWrite = false,
            ISerializationContext? context = null) where T : notnull
        {
            node = default;
            if (TryGetWriter<T>(context, out var writer))
            {
                node = writer.Write(this, obj, alwaysWrite, context);
                return true;
            }

            return false;
        }
        #endregion

        #region TryCopy
        private bool TryCopyWithTypeCopier(Type type, object source, ref object target, bool skipHook, ISerializationContext? context = null)
        {
            //TODO Paul: do this shit w/ delegates
            var method = typeof(SerializationManager).GetRuntimeMethods().First(m =>
                m.Name == nameof(TryCopyWithTypeCopier) && m.GetParameters().Length == 4).MakeGenericMethod(type, source.GetType(), target.GetType());

            var arr = new[] {source, target, skipHook, context};
            var res = method.Invoke(this, arr);

            if (res as bool? ?? false)
            {
                target = arr[1]!;
                return true;
            }

            return false;
        }

        private bool TryCopyWithTypeCopier<TCommon, TSource, TTarget>(
            TSource source,
            ref TTarget target,
            bool skipHook,
            ISerializationContext? context = null)
            where TSource : TCommon
            where TTarget : TCommon
            where TCommon : notnull
        {
            object? rawTypeCopier;

            if (context != null &&
                context.TypeCopiers.TryGetValue(typeof(TCommon), out rawTypeCopier) ||
                _typeCopiers.TryGetValue(typeof(TCommon), out rawTypeCopier))
            {
                var ser = (ITypeCopier<TCommon>) rawTypeCopier;
                target = (TTarget) ser.Copy(this, source, target, skipHook, context);
                return true;
            }

            if (TryGetGenericCopier(out ITypeCopier<TCommon>? genericTypeWriter))
            {
                target = (TTarget) genericTypeWriter.Copy(this, source, target, skipHook, context);
                return true;
            }

            return false;
        }
        #endregion

        #region Custom

        private DeserializationResult ReadWithCustomTypeSerializer<T, TNode, TSerializer>(TNode node, ISerializationContext? context = null, bool skipHook = false)
            where TSerializer : ITypeReader<T, TNode> where T : notnull where TNode : DataNode
        {
            var serializer = (ITypeReader<T, TNode>)GetTypeSerializer(typeof(TSerializer));
            return serializer.Read(this, node, DependencyCollection, skipHook, context);
        }

        private DataNode WriteWithCustomTypeSerializer<T, TSerializer>(T value,
            ISerializationContext? context = null, bool alwaysWrite = false)
            where TSerializer : ITypeWriter<T> where T : notnull
        {
            var serializer = (ITypeWriter<T>)GetTypeSerializer(typeof(TSerializer));
            return serializer.Write(this, value, alwaysWrite, context);
        }

        private TCommon CopyWithCustomTypeSerializer<TCommon, TSource, TTarget, TSerializer>(
            TSource source,
            TTarget target,
            bool skipHook,
            ISerializationContext? context = null)
            where TSource : TCommon
            where TTarget : TCommon
            where TCommon : notnull
            where TSerializer : ITypeCopier<TCommon>
        {
            var serializer = (ITypeCopier<TCommon>) GetTypeSerializer(typeof(TSerializer));
            return serializer.Copy(this, source, target, skipHook, context);
        }

        private ValidationNode ValidateWithCustomTypeSerializer<T, TNode, TSerializer>(
            TNode node,
            ISerializationContext? context)
            where T : notnull
            where TNode : DataNode
            where TSerializer : ITypeValidator<T, TNode>
        {
            var serializer = (ITypeValidator<T, TNode>) GetTypeSerializer(typeof(TSerializer));
            return serializer.Validate(this, node, DependencyCollection, context);
        }

        #endregion
    }
}
