using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.Manager
{
    public partial class Serv3Manager
    {
        private readonly Dictionary<Type, object> _typeSerializers = new();
        private readonly Dictionary<Type, Type> _genericSerializersTypes = new();

        private void InitializeTypeSerializers()
        {
            var typeSerializer = typeof(ITypeSerializer<>);
            foreach (var type in _reflectionManager.FindTypesWithAttribute<TypeSerializerAttribute>())
            {
                var interfaces = type.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeSerializer);
                foreach (var @interface in interfaces)
                {
                    var typeToSerialize = @interface.GetGenericArguments().First();

                    if (type.IsGenericTypeDefinition)
                    {
                        _genericSerializersTypes.Add(typeToSerialize, type);
                    }
                    else
                    {
                        CreateSerializer(typeToSerialize, type);
                    }
                }
            }
        }

        private object CreateSerializer(Type typeToSerialize, Type serializerType)
        {
            var serializer = Activator.CreateInstance(serializerType)!;
            IoCManager.InjectDependencies(serializer);
            _typeSerializers.Add(typeToSerialize,serializer);
            return serializer;
        }

        //TODO do with delegates?
        private bool TryGetGenericTypeSerializer(Type type, [NotNullWhen(true)] out object? rawTypeSer)
        {
            rawTypeSer = null;
            if (type.IsGenericType)
            {
                var typeDef = type.GetGenericTypeDefinition();
                Type? serializerTypeDef = null;
                foreach (var (key, val) in _genericSerializersTypes)
                {
                    if (typeDef.HasSameMetadataDefinitionAs(key))
                    {
                        serializerTypeDef = val;
                        break;
                    }
                }
                if (serializerTypeDef == null) return false;
                //if (!_genericSerializersTypes.TryGetValue(typeDef, out var serializerTypeDef)) return false;
                var serializerType = serializerTypeDef.MakeGenericType(type.GetGenericArguments());
                rawTypeSer = CreateSerializer(type, serializerType);
                return true;
            }

            return false;
        }

        private bool TryGetGenericTypeSerializer<T>([NotNullWhen(true)] out ITypeSerializer<T>? rawTypeSer)
        {
            rawTypeSer = null;
            if (typeof(T).IsGenericType)
            {
                var typeDef = typeof(T).GetGenericTypeDefinition();
                Type? serializerTypeDef = null;
                foreach (var (key, val) in _genericSerializersTypes)
                {
                    if (typeDef.HasSameMetadataDefinitionAs(key))
                    {
                        serializerTypeDef = val;
                        break;
                    }
                }
                if (serializerTypeDef == null) return false;
                var serializerType = serializerTypeDef.MakeGenericType(typeof(T).GetGenericArguments());
                rawTypeSer = (ITypeSerializer<T>)CreateSerializer(typeof(T), serializerType);
                return true;
            }

            return false;
        }

        private bool TryReadWithTypeSerializers(Type type, IDataNode node, [NotNullWhen(true)] out object? obj, ISerializationContext? context = null)
        {
            //TODO Paul: do this shit w/ delegates
            var method = typeof(Serv3Manager).GetRuntimeMethods().First(m =>
                m.Name == nameof(Serv3Manager.TryReadWithTypeSerializers) && m.GetParameters().Length == 3).MakeGenericMethod(type);
            obj = null;
            var arr = new object?[]
            {
                node,
                obj,
                context
            };
            var res = method.Invoke(this, arr);
            if ((res is bool ? (bool) res : false))
            {
                obj = arr[1];
                return true;
            }

            return false;
        }

        private bool TryReadWithTypeSerializers<T>(IDataNode node, out T? obj, ISerializationContext? context = null)
        {
            obj = default;

            if (TryGetGenericTypeSerializer(out ITypeSerializer<T>? genericTypeSer))
            {
                obj = genericTypeSer.NodeToType(node, context);
                return true;
            }

            if (!_typeSerializers.TryGetValue(typeof(T), out var rawTypeSer)) return false;
            var ser = (ITypeSerializer<T>) rawTypeSer;
            obj = ser.NodeToType(node, context);
            return true;
        }

        private bool TryWriteWithTypeSerializers(Type type, object obj, IDataNodeFactory nodeFactory, [NotNullWhen(true)] out IDataNode? node, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            //TODO Paul: do this shit w/ delegates
            var method = typeof(Serv3Manager).GetRuntimeMethods().First(m =>
                m.Name == nameof(Serv3Manager.TryWriteWithTypeSerializers) && m.GetParameters().Length == 5).MakeGenericMethod(type);
            node = null;
            var arr = new object?[]
            {
                obj,
                nodeFactory,
                node,
                alwaysWrite,
                context
            };
            var res = method.Invoke(this, arr);
            if ((res is bool ? (bool) res : false))
            {
                node = (IDataNode?)arr[2];
                return true;
            }

            return false;
        }

        private bool TryWriteWithTypeSerializers<T>(T obj, IDataNodeFactory nodeFactory, [NotNullWhen(true)] out IDataNode? node,
            bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            node = default;

            if (TryGetGenericTypeSerializer(out ITypeSerializer<T>? genericTypeSer))
            {
                node = genericTypeSer.TypeToNode(obj, nodeFactory, alwaysWrite, context);
                return true;
            }

            if (!_typeSerializers.TryGetValue(typeof(T), out var rawTypeSer)) return false;
            var ser = (ITypeSerializer<T>) rawTypeSer;
            node = ser.TypeToNode(obj, nodeFactory, alwaysWrite, context);
            return true;
        }
    }
}
