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

        private readonly Dictionary<(Type Type, Type DataNodeType), Type> _genericReaderTypes = new();
        private readonly Dictionary<Type, Type> _genericWriterTypes = new();

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

            if (readerInterfaces.Length == 0 && writerInterfaces.Length == 0)
            {
                throw new InvalidOperationException(
                    "Tried to register TypeReader/Writer that had none of the interfaces inherited.");
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
                return null;
            }
            else
            {
                var serializer = Activator.CreateInstance(type)!;
                IoCManager.InjectDependencies(serializer);

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

        private bool TryReadWithTypeSerializers<T, TNode>(TNode node, out DeserializationResult<T>? obj, ISerializationContext? context = null) where T : notnull where TNode : DataNode
        {
            if (_typeReaders.TryGetValue((typeof(T), typeof(TNode)), out var rawTypeReader))
            {
                var ser = (ITypeReader<T, TNode>) rawTypeReader;
                obj = ser.Read(node, context);
                return true;
            }

            if (TryGetGenericReader(out ITypeReader<T, TNode>? genericTypeReader))
            {
                obj = genericTypeReader.Read(node, context);
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

        private bool TryWriteWithTypeSerializers<T>(T obj, [NotNullWhen(true)] out DataNode? node,
            bool alwaysWrite = false,
            ISerializationContext? context = null) where T : notnull
        {
            node = default;

            if (_typeWriters.TryGetValue(typeof(T), out var rawTypeWriter))
            {
                var ser = (ITypeWriter<T>) rawTypeWriter;
                node = ser.Write(obj, alwaysWrite, context);
                return true;
            }

            if (TryGetGenericWriter(out ITypeWriter<T>? genericTypeWriter))
            {
                node = genericTypeWriter.Write(obj, alwaysWrite, context);
                return true;
            }

            return false;
        }
    }
}
