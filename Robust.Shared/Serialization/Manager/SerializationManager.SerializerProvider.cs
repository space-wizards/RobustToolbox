using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Serialization.Manager.Exceptions;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization.Manager;

public sealed partial class SerializationManager
{
    public static Type[] SerializerInterfaces => new[]
    {
        typeof(ITypeReader<,>),
        typeof(ITypeInheritanceHandler<,>),
        typeof(ITypeValidator<,>),
        typeof(ITypeCopyCreator<>),
        typeof(ITypeCopier<>),
        typeof(ITypeWriter<>)
    };

    private SerializerProvider _regularSerializerProvider = default!;

    private void InitializeTypeSerializers(IEnumerable<Type> typeSerializers)
    {
        _regularSerializerProvider = new(typeSerializers);
    }

    private static object CreateSerializer(Type type)
    {
        DebugTools.Assert(!type.IsGenericTypeDefinition);
        DebugTools.Assert(!type.IsAbstract);

        return Activator.CreateInstance(type)!;
    }

    public sealed class SerializerProvider
    {
        public SerializerProvider(IEnumerable<Type> typeSerializers)
        {
            foreach (var serializerInterface in SerializerInterfaces)
            {
                RegisterSerializerInterface(serializerInterface);
            }

            foreach (var typeSerializer in typeSerializers)
            {
                RegisterSerializer(typeSerializer);
            }
        }

        public SerializerProvider()
        {
            foreach (var serializerInterface in SerializerInterfaces)
            {
                RegisterSerializerInterface(serializerInterface);
            }
        }

        private Dictionary<Type, Dictionary<(Type ObjectType, Type NodeType), object>> _typeNodeSerializers = new();
        private Dictionary<Type, Dictionary<Type, object>> _typeSerializers = new();
        private Dictionary<Type, Dictionary<(Type ObjectType, Type NodeType), Type>> _genericTypeNodeSerializers = new();
        private Dictionary<Type, Dictionary<Type, Type>> _genericTypeSerializers = new();

        private List<Type> _typeNodeInterfaces = new();
        private List<Type> _typeInterfaces = new();

        private readonly object _lock = new();

        #region GetSerializerMethods

        public bool TryGetTypeNodeSerializer<TInterface, TType, TNode>([NotNullWhen(true)] out TInterface? serializer)
            where TInterface : BaseSerializerInterfaces.ITypeNodeInterface<TType, TNode>
            where TNode : DataNode
        {
            serializer = default;
            if (!TryGetTypeNodeSerializer(typeof(TInterface).GetGenericTypeDefinition(), typeof(TType), typeof(TNode), out var rawSerializer))
                return false;

            serializer = (TInterface)rawSerializer;
            return true;
        }

        public bool TryGetTypeNodeSerializer(Type interfaceType, Type objectType, Type nodeType, [NotNullWhen(true)] out object? serializer)
        {
            lock (_lock)
            {
                if (_typeNodeSerializers.TryGetValue(interfaceType, out var typeNodeSerializers) &&
                    typeNodeSerializers.TryGetValue((objectType, nodeType), out serializer))
                    return true;

                if (_genericTypeNodeSerializers.TryGetValue(interfaceType, out var genericTypeNodeSerializers) &&
                    objectType.IsGenericType)
                {
                    var typeDef = objectType.GetGenericTypeDefinition();
                    foreach (var (key, val) in genericTypeNodeSerializers)
                    {
                        if (typeDef.HasSameMetadataDefinitionAs(key.ObjectType) && nodeType == key.NodeType)
                        {
                            var serializerType = val.MakeGenericType(objectType.GetGenericArguments());
                            serializer = RegisterSerializer(serializerType)!;
                            return true;
                        }
                    }
                }

                serializer = null;
                return false;
            }
        }

        public TInterface GetTypeNodeSerializer<TInterface, TType, TNode>()
            where TInterface : BaseSerializerInterfaces.ITypeNodeInterface<TType, TNode>
            where TNode : DataNode
        {
            if (!TryGetTypeNodeSerializer<TInterface, TType, TNode>(out var serializer))
                throw new ArgumentOutOfRangeException();

            return serializer;
        }

        public object GetTypeNodeSerializer(Type interfaceType, Type objectType, Type nodeType)
        {
            if (!TryGetTypeNodeSerializer(interfaceType, objectType, nodeType, out var serializer))
                throw new ArgumentOutOfRangeException();

            return serializer;
        }

        public bool TryGetTypeSerializer<TInterface, TType>([NotNullWhen(true)] out TInterface? serializer)
            where TInterface : BaseSerializerInterfaces.ITypeInterface<TType>
        {
            serializer = default;
            if (!TryGetTypeSerializer(typeof(TInterface).GetGenericTypeDefinition(), typeof(TType), out var rawSerializer))
                return false;

            serializer = (TInterface)rawSerializer;
            return true;
        }

        public bool TryGetTypeSerializer(Type interfaceType, Type objectType, [NotNullWhen(true)] out object? serializer)
        {
            lock (_lock)
            {
                if (_typeSerializers.TryGetValue(interfaceType, out var typeSerializers) &&
                    typeSerializers.TryGetValue(objectType, out serializer))
                    return true;

                if (_genericTypeSerializers.TryGetValue(interfaceType, out var genericTypeSerializers) &&
                    objectType.IsGenericType)
                {
                    var typeDef = objectType.GetGenericTypeDefinition();
                    foreach (var (key, val) in genericTypeSerializers)
                    {
                        if (typeDef.HasSameMetadataDefinitionAs(key))
                        {
                            var serializerType = val.MakeGenericType(objectType.GetGenericArguments());
                            serializer = RegisterSerializer(serializerType)!;
                            return true;
                        }
                    }
                }

                serializer = null;
                return false;
            }
        }

        public TInterface GetTypeSerializer<TInterface, TType>()
            where TInterface : BaseSerializerInterfaces.ITypeInterface<TType>
        {
            if (!TryGetTypeSerializer<TInterface, TType>(out var serializer))
                throw new ArgumentOutOfRangeException();

            return serializer;
        }

        public object GetTypeSerializer(Type interfaceType, Type objectType)
        {
            if (!TryGetTypeSerializer(interfaceType, objectType, out var serializer))
                throw new ArgumentOutOfRangeException();

            return serializer;
        }

        #endregion

        #region RegisterMethods

        public object RegisterSerializer(object obj) => RegisterSerializer(obj.GetType(), obj);

        private object RegisterSerializer(Type type, object obj)
        {
            lock (_lock)
            {
                foreach (var @interface in type.GetInterfaces())
                {
                    if (!@interface.IsGenericType) continue;

                    foreach (var typeInterface in _typeInterfaces)
                    {
                        if (@interface.GetGenericTypeDefinition().HasSameMetadataDefinitionAs(typeInterface))
                        {
                            var arguments = @interface.GetGenericArguments();
                            if (arguments.Length != 1)
                                throw new InvalidGenericParameterCountException();
                            _typeSerializers.GetOrNew(typeInterface).Add(arguments[0], obj);
                        }
                    }

                    foreach (var typeInterface in _typeNodeInterfaces)
                    {
                        if (@interface.GetGenericTypeDefinition().HasSameMetadataDefinitionAs(typeInterface))
                        {
                            var arguments = @interface.GetGenericArguments();
                            if (arguments.Length != 2)
                                throw new InvalidGenericParameterCountException();
                            _typeNodeSerializers.GetOrNew(typeInterface).Add((arguments[0], arguments[1]), obj);
                        }
                    }
                }

                return obj;
            }
        }

        public T? RegisterSerializer<T>() => (T?)RegisterSerializer(typeof(T));

        public object? RegisterSerializer(Type type)
        {
            lock (_lock)
            {
                if (type.IsGenericTypeDefinition)
                {
                    var typeArguments = type.GetGenericArguments();
                    foreach (var @interface in type.GetInterfaces())
                    {
                        foreach (var typeInterface in _typeInterfaces)
                        {
                            if (@interface.GetGenericTypeDefinition().HasSameMetadataDefinitionAs(typeInterface))
                            {
                                var arguments = @interface.GetGenericArguments();
                                if (arguments.Length != 1)
                                    throw new InvalidGenericParameterCountException();
                                var objArguments = arguments[0].GetGenericArguments();
                                for (int i = 0; i < typeArguments.Length; i++)
                                {
                                    if (typeArguments[i] != objArguments[i])
                                        throw new GenericParameterMismatchException();
                                }

                                _genericTypeSerializers.GetOrNew(typeInterface).Add(arguments[0], type);
                            }
                        }

                        foreach (var typeInterface in _typeNodeInterfaces)
                        {
                            if (@interface.GetGenericTypeDefinition().HasSameMetadataDefinitionAs(typeInterface))
                            {
                                var arguments = @interface.GetGenericArguments();
                                if (arguments.Length != 2)
                                    throw new InvalidGenericParameterCountException();
                                var objArguments = arguments[0].GetGenericArguments();
                                for (int i = 0; i < typeArguments.Length; i++)
                                {
                                    if (typeArguments[i] != objArguments[i])
                                        throw new GenericParameterMismatchException();
                                }

                                _genericTypeNodeSerializers.GetOrNew(typeInterface)
                                    .Add((arguments[0], arguments[1]), type);
                            }
                        }
                    }

                    return null;
                }

                return RegisterSerializer(type, CreateSerializer(type));
            }
        }

        //todo paul serv3 is there a better way than comparing names here?
        private void RegisterSerializerInterface(Type type)
        {
            if (!type.IsGenericTypeDefinition)
                throw new ArgumentException("Only generic type definitions can be signed up as interfaces", nameof(type));

            // Note: lock is entered recursively.
            lock (_lock)
            {
                var genericTypeNode = typeof(BaseSerializerInterfaces.ITypeNodeInterface<,>);
                var genericType = typeof(BaseSerializerInterfaces.ITypeInterface<>);
                var genericParams = type.GetGenericArguments();
                foreach (var @interface in type.GetInterfaces())
                {
                    var genericInterface = @interface.GetGenericTypeDefinition();
                    if (genericInterface.HasSameMetadataDefinitionAs(genericTypeNode))
                    {
                        var genericInterfaceParams = genericInterface.GetGenericArguments();
                        for (int i = 0; i < genericParams.Length; i++)
                        {
                            if (genericParams[i].Name != genericInterfaceParams[i].Name)
                                throw new GenericParameterMismatchException();
                        }

                        _typeNodeInterfaces.Add(type);
                    }
                    else if (genericInterface.HasSameMetadataDefinitionAs(genericType))
                    {
                        var genericInterfaceParams = genericInterface.GetGenericArguments();
                        for (int i = 0; i < genericParams.Length; i++)
                        {
                            if (genericParams[i].Name != genericInterfaceParams[i].Name)
                                throw new GenericParameterMismatchException();
                        }

                        _typeInterfaces.Add(type);
                    }
                }
            }
        }

        #endregion
    }

}
