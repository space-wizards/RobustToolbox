using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
                if (!_genericSerializersTypes.TryGetValue(typeDef, out var serializerTypeDef)) return false;
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
                if (!_genericSerializersTypes.TryGetValue(typeDef, out var serializerTypeDef)) return false;
                var serializerType = serializerTypeDef.MakeGenericType(typeof(T).GetGenericArguments());
                rawTypeSer = (ITypeSerializer<T>)CreateSerializer(typeof(T), serializerType);
                return true;
            }

            return false;
        }

        private bool TryReadWithTypeSerializers(Type type, IDataNode node, out object? obj)
        {
            //TODO Paul: do this shit w/ delegates
            throw new NotImplementedException();
        }

        private bool TryReadWithTypeSerializers<T>(IDataNode node, out T? obj, ISerializationContext? context = null)
        {
            obj = default;
            if (!_typeSerializers.TryGetValue(typeof(T), out var rawTypeSer)) return false;
            var ser = (ITypeSerializer<T>) rawTypeSer;
            obj = ser.NodeToType(node, context);
            return true;
        }

        private bool TryWriteWithTypeSerializers(object obj, IDataNodeFactory nodeFactory, [NotNullWhen(true)] out IDataNode? node, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            //TODO Paul: do this shit w/ delegates
            throw new NotImplementedException();
        }

        private bool TryWriteWithTypeSerializers<T>(T obj, IDataNodeFactory nodeFactory, [NotNullWhen(true)] out IDataNode? node,
            bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            node = default;
            if (!_typeSerializers.TryGetValue(typeof(T), out var rawTypeSer)) return false;
            var ser = (ITypeSerializer<T>) rawTypeSer;
            node = ser.TypeToNode(obj, nodeFactory, alwaysWrite, context);
            return true;
        }
    }
}
