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

namespace Robust.Shared.Serialization.Manager
{
    public partial class SerializationManager
    {
        private readonly Dictionary<(Type Type, Type DataNodeType), object> _typeReaders = new();
        private readonly Dictionary<Type, object> _typeWriters = new();
        private readonly Dictionary<Type, object> _typeCopiers = new();

        private readonly Dictionary<(Type Type, Type DataNodeType), Type> _genericReaderTypes = new();
        private readonly Dictionary<Type, Type> _genericWriterTypes = new();
        private readonly Dictionary<Type, Type> _genericCopierTypes = new();

        private void InitializeTypeSerializers()
        {
            foreach (var type in _reflectionManager.FindTypesWithAttribute<TypeSerializerAttribute>())
            {
                RegisterSerializer(type);
            }
        }

        // TODO Paul register copiers
        private object? RegisterSerializer(Type type)
        {
            var writerInterfaces = type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ITypeWriter<>)).ToArray();
            var readerInterfaces = type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ITypeReader<,>)).ToArray();
            var copierInterfaces = type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ITypeCopier<>)).ToArray();

            if (readerInterfaces.Length == 0 && writerInterfaces.Length == 0 && copierInterfaces.Length == 0)
            {
                throw new InvalidOperationException(
                    "Tried to register TypeReader/Writer/Copier that had none of the interfaces inherited.");
            }

            if (type.IsGenericTypeDefinition)
            {
                foreach (var writerInterface in writerInterfaces)
                {
                    if(!_genericWriterTypes.TryAdd(writerInterface.GetGenericArguments()[0], type))
                        Logger.Error($"Tried registering generic writer for type {writerInterface.GetGenericArguments()[0]} twice");
                }

                foreach (var readerInterface in readerInterfaces)
                {
                    if(!_genericReaderTypes.TryAdd((readerInterface.GetGenericArguments()[0], readerInterface.GetGenericArguments()[1]), type))
                        Logger.Error($"Tried registering generic reader for type {readerInterface.GetGenericArguments()[0]} and datanode {readerInterface.GetGenericArguments()[1]} twice");
                }

                foreach (var copierInterface in copierInterfaces)
                {
                    if(!_genericCopierTypes.TryAdd(copierInterface.GetGenericArguments()[0], type))
                        Logger.Error($"Tried registering generic copier for type {copierInterface.GetGenericArguments()[0]} twice");
                }

                return null;
            }
            else
            {
                var serializer = Activator.CreateInstance(type)!;

                foreach (var writerInterface in writerInterfaces)
                {
                    if(!_typeWriters.TryAdd(writerInterface.GetGenericArguments()[0], serializer))
                        Logger.Error($"Tried registering writer for type {writerInterface.GetGenericArguments()[0]} twice");
                }

                foreach (var readerInterface in readerInterfaces)
                {
                    if(!_typeReaders.TryAdd((readerInterface.GetGenericArguments()[0], readerInterface.GetGenericArguments()[1]), serializer))
                        Logger.Error($"Tried registering reader for type {readerInterface.GetGenericArguments()[0]} and datanode {readerInterface.GetGenericArguments()[1]} twice");
                }

                foreach (var copierInterface in copierInterfaces)
                {
                    if(!_typeCopiers.TryAdd(copierInterface.GetGenericArguments()[0], serializer))
                        Logger.Error($"Tried registering copier for type {copierInterface.GetGenericArguments()[0]} twice");
                }

                return serializer;
            }
        }

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
                rawReader = (ITypeReader<T, TNode>)RegisterSerializer(serializerType)!;
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
                rawWriter = (ITypeWriter<T>)RegisterSerializer(serializerType)!;
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
                rawCopier = (ITypeCopier<T>)RegisterSerializer(serializerType)!;
                return true;
            }

            return false;
        }

        private bool TryReadWithTypeSerializers(Type type, DataNode node, [NotNullWhen(true)] out DeserializationResult? obj, ISerializationContext? context = null)
        {
            //TODO Paul: do this shit w/ delegates
            var method = typeof(SerializationManager).GetRuntimeMethods().First(m =>
                m.Name == nameof(TryReadWithTypeSerializers) && m.GetParameters().Length == 3).MakeGenericMethod(type, node.GetType());

            obj = null;

            var arr = new object?[] {node, obj, context};
            var res = method.Invoke(this, arr);

            if (res as bool? ?? false)
            {
                obj = (DeserializationResult?)arr[1];
                return true;
            }

            return false;
        }

        private bool TryValidateWithTypeReader(Type type, DataNode node, ISerializationContext? context, out bool valid)
        {
            //TODO Paul: do this shit w/ delegates
            var method = typeof(SerializationManager).GetRuntimeMethods().First(m =>
                m.Name == nameof(TryValidateWithTypeReader) && m.GetParameters().Length == 2).MakeGenericMethod(type, node.GetType());

            var arr = new object?[] {node, context, false};
            var res = method.Invoke(this, arr);

            if (res as bool? ?? false)
            {
                valid = (bool)arr[2]!;
                return true;
            }

            valid = false;
            return false;
        }

        private bool TryValidateWithTypeReader<T, TNode>(TNode node, ISerializationContext? context, out bool valid) where T : notnull where TNode : DataNode
        {
            if (TryGetReader<T, TNode>(null, out var reader))
            {
                valid = reader.Validate(this, node, context);
                return true;
            }

            valid = false;
            return false;
        }

        private bool TryGetReader<T, TNode>(ISerializationContext? context, [NotNullWhen(true)] out ITypeReader<T, TNode>? reader)
            where T : notnull where TNode : DataNode
        {
            if (context != null && context.TypeReaders.TryGetValue((typeof(T), typeof(TNode)), out var rawTypeReader) ||
                _typeReaders.TryGetValue((typeof(T), typeof(TNode)), out rawTypeReader))
            {
                reader = (ITypeReader<T, TNode>) rawTypeReader;
                return true;
            }

            return TryGetGenericReader(out reader);
        }

        private bool TryReadWithTypeSerializers<T, TNode>(TNode node, out DeserializationResult? obj, ISerializationContext? context = null) where T : notnull where TNode : DataNode
        {
            if (TryGetReader<T, TNode>(context, out var reader))
            {
                obj = reader.Read(this, node, context);
                return true;
            }

            obj = null;
            return false;
        }

        private bool TryWriteWithTypeSerializers(Type type, object obj, [NotNullWhen(true)] out DataNode? node, bool alwaysWrite = false,
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
                node = (DataNode?) arr[1];
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

        private bool TryWriteWithTypeSerializers<T>(T obj, [NotNullWhen(true)] out DataNode? node,
            bool alwaysWrite = false,
            ISerializationContext? context = null) where T : notnull
        {
            node = default;
            if (TryGetWriter<T>(context, out var writer))
            {
                node = writer.Write(this, obj, alwaysWrite, context);

            }

            return false;
        }

        private bool TryCopyWithTypeCopier(Type type, object source, ref object target, ISerializationContext? context = null)
        {
            //TODO Paul: do this shit w/ delegates
            var method = typeof(SerializationManager).GetRuntimeMethods().First(m =>
                m.Name == nameof(TryCopyWithTypeCopier) && m.GetParameters().Length == 3).MakeGenericMethod(type, source.GetType(), target.GetType());

            var arr = new[] {source, target, context};
            var res = method.Invoke(this, arr);

            if (res as bool? ?? false)
            {
                target = arr[1]!;
                return true;
            }

            return false;
        }

        private bool TryCopyWithTypeCopier<TCommon, TSource, TTarget>(TSource source, ref TTarget target, ISerializationContext? context = null)
            where TSource : TCommon where TTarget : TCommon where TCommon : notnull
        {
            object? rawTypeCopier;
            if (context != null && context.TypeCopiers.TryGetValue(typeof(TCommon), out rawTypeCopier) ||
                _typeCopiers.TryGetValue(typeof(TCommon), out rawTypeCopier))
            {
                var ser = (ITypeCopier<TCommon>) rawTypeCopier;
                target = (TTarget) ser.Copy(this, source, target, context);
                return true;
            }

            if (TryGetGenericCopier(out ITypeCopier<TCommon>? genericTypeWriter))
            {
                target = (TTarget) genericTypeWriter.Copy(this, source, target, context);
                return true;
            }

            return false;
        }
    }
}
