using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Exceptions;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization.Manager;

public sealed partial class SerializationManager
{
    /// <summary>
    ///     <see cref="CopyCreatorIndex"/>
    ///     <see cref="CopierIndex"/>
    /// </summary>
    private static readonly ImmutableArray<Type> SerializerInterfaces = new[]
    {
        typeof(ITypeReader<,>),
        typeof(ITypeInheritanceHandler<,>),
        typeof(ITypeValidator<,>),
        typeof(ITypeCopyCreator<>),
        typeof(ITypeCopier<>),
        typeof(ITypeWriter<>)
    }.ToImmutableArray();

    /// <summary>
    ///     <see cref="SerializerInterfaces"/>
    /// </summary>
    private const int CopyCreatorIndex = 3;

    /// <summary>
    ///     <see cref="SerializerInterfaces"/>
    /// </summary>
    private const int CopierIndex = 4;

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

    [Obsolete]
    public bool TryGetCopierOrCreator<TType>(out ITypeCopier<TType>? copier, out ITypeCopyCreator<TType>? copyCreator, ISerializationContext? context = null)
    {
        if (context != null)
        {
            context.SerializerProvider.TryGetCopierOrCreator(out copier, out copyCreator);
            if (copier != null || copyCreator != null)
                return true;
        }

        _regularSerializerProvider.TryGetCopierOrCreator(out copier, out copyCreator);
        return copier != null || copyCreator != null;
    }

    [Obsolete]
    public bool TryCustomCopy<T>(T source, ref T target, SerializationHookContext hookCtx,  bool hasHooks, ISerializationContext? context = null)
    {
        if (TryGetCopierOrCreator<T>(out var copier, out var copyCreator))
        {
            if (copier != null)
            {
                CopyTo(copier, source, ref target, hookCtx, context);
                return true;
            }

            target = CreateCopy(copyCreator!, source, hookCtx, context);
            return true;
        }

        return false;
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

        // TODO make this a 1d array containing the 6 interfaces
        /// <summary>
        ///     Type serializers indexed by their type serializer and type
        ///     that they serialize.
        ///     <see cref="SerializationManager.SerializerInterfaces"/> for the first index.
        /// </summary>
        private (object? Regular, object? Generic)[]?[] _typeSerializersArray = new (object? Regular, object? Generic)[]?[] { };

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
                            RegisterIndexedSerializer(objectType, SerializerInterfaces.IndexOf(interfaceType), serializer, false);
                            return true;
                        }
                    }
                }

                serializer = null;
                return false;
            }
        }

        internal bool TryGetCopierOrCreator<TType>(out ITypeCopier<TType>? copier, out ITypeCopyCreator<TType>? copyCreator)
        {
            copier = null;
            copyCreator = null;

            var information = SerializedType<TType>.Information;
            if (information.Id >= _typeSerializersArray.Length)
                return false;

            var serializerArray = _typeSerializersArray[information.Id];
            if (serializerArray == null)
                return false;

            var copiers = serializerArray[CopierIndex];
            var copyCreators = serializerArray[CopyCreatorIndex];
            copier = Unsafe.As<ITypeCopier<TType>?>(copiers.Regular);
            copyCreator = Unsafe.As<ITypeCopyCreator<TType>?>(copyCreators.Regular);

            if (copier != null || copyCreator != null)
                return true;

            copier = Unsafe.As<ITypeCopier<TType>?>(copiers.Generic);
            copyCreator = Unsafe.As<ITypeCopyCreator<TType>?>(copyCreators.Generic);

            return copier != null || copyCreator != null;
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

                    for (var i = 0; i < _typeInterfaces.Count; i++)
                    {
                        var typeInterface = _typeInterfaces[i];
                        if (@interface.GetGenericTypeDefinition().HasSameMetadataDefinitionAs(typeInterface))
                        {
                            var arguments = @interface.GetGenericArguments();
                            if (arguments.Length != 1)
                                throw new InvalidGenericParameterCountException();
                            _typeSerializers.GetOrNew(typeInterface).Add(arguments[0], obj);
                            RegisterIndexedSerializer(arguments[0], SerializerInterfaces.IndexOf(typeInterface), obj, true);
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

        private void RegisterIndexedSerializer(Type elementType, int interfaceIndex, object serializer, bool regular)
        {
            var id = SerializedType.GetId(elementType);
            if (id >= _typeSerializers.Count)
            {
                Array.Resize(ref _typeSerializersArray, (id + 1) * 2);
            }

            var array = new (object? Regular, object? Generic)[SerializerInterfaces.Length];
            _typeSerializersArray[id] = array;

            if (regular)
            {
                array[interfaceIndex].Regular = serializer;
            }
            else
            {
                array[interfaceIndex].Generic = serializer;
            }
        }

        #endregion
    }

    private static class SerializedType
    {
        internal static int Id;
        private static readonly object Lock = new();

        internal static int GetId(Type type)
        {
            lock (Lock)
            {
                var serializedType = typeof(SerializedType<>).MakeGenericType(type);
                var field = serializedType.GetField("Information", BindingFlags.Static | BindingFlags.NonPublic);
                var information = (TypeInformation) field!.GetValue(null)!;
                return information.Id;
            }
        }
    }

    private static class SerializedType<T>
    {
        // ReSharper disable once StaticMemberInGenericType
        internal static readonly TypeInformation Information;

        static SerializedType()
        {
            var type = typeof(T);
            var returnSource = type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(Type) || type.IsDefined(typeof(CopyByRefAttribute), true);
            var serializationGenerated = type.IsAssignableTo(typeof(ISerializationGenerated<T>));
            Information = new TypeInformation(Interlocked.Increment(ref SerializedType.Id), returnSource, serializationGenerated);
        }
    }

    private readonly struct TypeInformation
    {
        internal readonly int Id;
        internal readonly bool ReturnSource;
        internal readonly bool SerializationGenerated;

        public TypeInformation(int id, bool returnSource, bool serializationGenerated)
        {
            Id = id;
            ReturnSource = returnSource;
            SerializationGenerated = serializationGenerated;
        }
    }
}
