using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Exceptions;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
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

    private static readonly ImmutableArray<Type> Nodes = new[]
    {
        typeof(MappingDataNode),
        typeof(SequenceDataNode),
        typeof(ValueDataNode),
    }.ToImmutableArray();

    /// <summary>
    ///     <see cref="SerializerInterfaces"/>
    /// </summary>
    private const int ReaderIndex = 0;

    /// <summary>
    ///     <see cref="SerializerInterfaces"/>
    /// </summary>
    private const int InheritanceHandlerIndex = 1;

    /// <summary>
    ///     <see cref="SerializerInterfaces"/>
    /// </summary>
    private const int ValidatorIndex = 2;

    /// <summary>
    ///     <see cref="SerializerInterfaces"/>
    /// </summary>
    private const int CopyCreatorIndex = 3;

    /// <summary>
    ///     <see cref="SerializerInterfaces"/>
    /// </summary>
    private const int CopierIndex = 4;

    /// <summary>
    ///     <see cref="SerializerInterfaces"/>
    /// </summary>
    private const int WriterIndex = 5;

    /// <summary>
    ///     <see cref="Nodes"/>
    /// </summary>
    private const int MappingIndex = 0;

    /// <summary>
    ///     <see cref="Nodes"/>
    /// </summary>
    private const int SequenceIndex = 1;

    /// <summary>
    ///     <see cref="Nodes"/>
    /// </summary>
    private const int ValueIndex = 2;

    private SerializerProvider _regularSerializerProvider = default!;

    private ISawmill _serializerSawmill = default!;

    private void InitializeTypeSerializers(IEnumerable<Type> typeSerializers)
    {
        DebugTools.AssertEqual(ReaderIndex, SerializerInterfaces.IndexOf(typeof(ITypeReader<,>)));
        DebugTools.AssertEqual(InheritanceHandlerIndex, SerializerInterfaces.IndexOf(typeof(ITypeInheritanceHandler<,>)));
        DebugTools.AssertEqual(ValidatorIndex, SerializerInterfaces.IndexOf(typeof(ITypeValidator<,>)));
        DebugTools.AssertEqual(CopyCreatorIndex, SerializerInterfaces.IndexOf(typeof(ITypeCopyCreator<>)));
        DebugTools.AssertEqual(CopierIndex, SerializerInterfaces.IndexOf(typeof(ITypeCopier<>)));

        DebugTools.AssertEqual(MappingIndex, Nodes.IndexOf(typeof(MappingDataNode)));
        DebugTools.AssertEqual(SequenceIndex, Nodes.IndexOf(typeof(SequenceDataNode)));
        DebugTools.AssertEqual(ValueIndex, Nodes.IndexOf(typeof(ValueDataNode)));

        _regularSerializerProvider = new(this, typeSerializers);
    }

    private object CreateSerializer(Type type)
    {
        DebugTools.Assert(!type.IsGenericTypeDefinition);
        DebugTools.Assert(!type.IsAbstract);

        var result = Activator.CreateInstance(type)!;
        DependencyCollection.InjectDependencies(result);
        if (result is BaseTypeSerializer ser)
        {
            ser.SerMan = this;
            ser.Log = _serializerSawmill;
        }

        if (result is IPostInjectInit postInject)
            postInject.PostInject();

        return result;
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
        if (target != null && source is ISerializationGenerated)
            return false;

        if (TryGetCopierOrCreator<T>(out var copier, out var copyCreator, context))
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
        private readonly SerializationManager _ser;

        public SerializerProvider(ISerializationManager ser, IEnumerable<Type> typeSerializers) : this(ser)
        {
            foreach (var typeSerializer in typeSerializers)
            {
                RegisterSerializer(typeSerializer);
            }
        }

        public SerializerProvider(ISerializationManager ser)
        {
            // cast it here so every user of this can just directly pass it from a [Dependency] without casting it themselves
            _ser = (SerializationManager) ser;
            foreach (var serializerInterface in SerializerInterfaces)
            {
                RegisterSerializerInterface(serializerInterface);
            }
        }

        private (object? Regular, object? Generic, bool Init)[] _typeNodeSerializersArray = [];
        private readonly ConcurrentDictionary<Type, ConcurrentDictionary<(Type ObjectType, Type NodeType), object>> _typeNodeSerializers = new();
        private readonly ConcurrentDictionary<Type, ConcurrentDictionary<Type, object>> _typeSerializers = new();

        // TODO make this a 1d array containing the 6 interfaces
        /// <summary>
        ///     Type serializers indexed by their type serializer and type
        ///     that they serialize.
        ///     <see cref="SerializationManager.SerializerInterfaces"/> for the first index.
        /// </summary>
        private (object? Regular, object? Generic)[]?[] _typeSerializersArray = [];

        private readonly ConcurrentDictionary<Type, ConcurrentDictionary<(Type ObjectType, Type NodeType), Type>> _genericTypeNodeSerializers = new();
        private readonly ConcurrentDictionary<Type, ConcurrentDictionary<Type, Type>> _genericTypeSerializers = new();

        private readonly List<Type> _typeNodeInterfaces = new();
        private readonly List<Type> _typeInterfaces = new();

        private readonly Lock _lock = new();

        #region GetSerializerMethods

        public bool TryGetTypeNodeSerializer<TInterface, TType, TNode>([NotNullWhen(true)] out TInterface? serializer)
            where TInterface : BaseSerializerInterfaces.ITypeNodeInterface<TType, TNode>
            where TNode : DataNode
        {
            serializer = default;
            object? rawSerializer;
            var index = TypeSerializerType<TInterface, TType, TNode>.Index;
            if (index < _typeNodeSerializersArray.Length)
            {
                ref var serializers = ref _typeNodeSerializersArray[index];
                if (serializers.Init)
                {
                    if (serializers.Regular != null)
                    {
                        serializer = (TInterface) serializers.Regular;
                        return true;
                    }

                    if (serializers.Generic != null)
                    {
                        serializer = (TInterface) serializers.Generic;
                        return true;
                    }

                    return false;
                }

                if (TryGetTypeNodeSerializer(typeof(TInterface).GetGenericTypeDefinition(),
                        typeof(TType),
                        typeof(TNode),
                        out rawSerializer))
                {
                    serializer = (TInterface) rawSerializer;
                    return true;
                }

                serializers.Init = true;
                return false;
            }

            if (TryGetTypeNodeSerializer(typeof(TInterface).GetGenericTypeDefinition(),
                    typeof(TType),
                    typeof(TNode),
                    out rawSerializer))
            {
                serializer = (TInterface) rawSerializer;
                return true;
            }

            return false;
        }

        internal bool TryGetTypeNodeSerializerArray<TInterface, TType, TNode>([NotNullWhen(true)] out TInterface? serializer)
            where TInterface : BaseSerializerInterfaces.ITypeNodeInterface<TType[], TNode>
            where TNode : DataNode
        {
            serializer = default;
            if (!TryGetTypeNodeSerializer(typeof(TInterface).GetGenericTypeDefinition(), typeof(TType[]), typeof(TNode), out var rawSerializer))
                return false;

            serializer = (TInterface)rawSerializer;
            return true;
        }

        public bool TryGetTypeNodeSerializer(Type interfaceType, Type objectType, Type nodeType, [NotNullWhen(true)] out object? serializer)
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
                    if (!typeDef.HasSameMetadataDefinitionAs(key.ObjectType) || nodeType != key.NodeType)
                        continue;

                    var serializerType = val.MakeGenericType(objectType.GetGenericArguments());
                    serializer = RegisterSerializer(serializerType)!;
                    RegisterIndexedNodeSerializer(interfaceType, objectType, key.NodeType, serializer, false);
                    return true;
                }
            }

            serializer = null;
            return false;
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
            if (_typeSerializers.TryGetValue(interfaceType, out var typeSerializers) &&
                typeSerializers.TryGetValue(objectType, out serializer))
                return true;

            if (_genericTypeSerializers.TryGetValue(interfaceType, out var genericTypeSerializers) &&
                objectType.IsGenericType)
            {
                var typeDef = objectType.GetGenericTypeDefinition();
                foreach (var (key, val) in genericTypeSerializers)
                {
                    if (!typeDef.HasSameMetadataDefinitionAs(key))
                        continue;

                    var serializerType = val.MakeGenericType(objectType.GetGenericArguments());
                    serializer = RegisterSerializer(serializerType)!;
                    RegisterIndexedSerializer(
                        objectType,
                        SerializerInterfaces.IndexOf(interfaceType),
                        serializer,
                        false
                    );

                    return true;
                }
            }

            serializer = null;
            return false;
        }

        internal bool TryGetCopierOrCreator<TType>(out ITypeCopier<TType>? copier, out ITypeCopyCreator<TType>? copyCreator)
        {
            copier = null;
            copyCreator = null;

            var information = SerializedType<TType>.Information;
            if (information.Id < _typeSerializersArray.Length &&
                _typeSerializersArray[information.Id] is { } serializerArray)
            {
                var copiers = serializerArray[CopierIndex];
                var copyCreators = serializerArray[CopyCreatorIndex];
                copier = Unsafe.As<ITypeCopier<TType>?>(copiers.Regular);
                copyCreator = Unsafe.As<ITypeCopyCreator<TType>?>(copyCreators.Regular);

                if (copier != null || copyCreator != null)
                    return true;

                copier = Unsafe.As<ITypeCopier<TType>?>(copiers.Generic);
                copyCreator = Unsafe.As<ITypeCopyCreator<TType>?>(copyCreators.Generic);

                if (copier != null || copyCreator != null)
                    return true;
            }

            if (TryGetTypeSerializer(typeof(ITypeCopier<>), typeof(TType), out var rawCopier))
                copier = (ITypeCopier<TType>) rawCopier;

            if (TryGetTypeSerializer(typeof(ITypeCopyCreator<>), typeof(TType), out var rawCopyCreator))
                copyCreator = (ITypeCopyCreator<TType>) rawCopyCreator;

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
            foreach (var @interface in type.GetInterfaces())
            {
                if (!@interface.IsGenericType) continue;

                foreach (var typeInterface in _typeInterfaces)
                {
                    if (!@interface.GetGenericTypeDefinition().HasSameMetadataDefinitionAs(typeInterface))
                        continue;

                    var arguments = @interface.GetGenericArguments();
                    if (arguments.Length != 1)
                        throw new InvalidGenericParameterCountException();

                    _typeSerializers.GetOrNew(typeInterface).TryAdd(arguments[0], obj);
                    RegisterIndexedSerializer(
                        arguments[0],
                        SerializerInterfaces.IndexOf(typeInterface),
                        obj,
                        true
                    );
                }

                foreach (var typeInterface in _typeNodeInterfaces)
                {
                    if (!@interface.GetGenericTypeDefinition().HasSameMetadataDefinitionAs(typeInterface))
                        continue;

                    var arguments = @interface.GetGenericArguments();
                    if (arguments.Length != 2)
                        throw new InvalidGenericParameterCountException();

                    _typeNodeSerializers.GetOrAdd(typeInterface, _ => new())
                        .TryAdd((arguments[0], arguments[1]), obj);
                    RegisterIndexedNodeSerializer(
                        typeInterface,
                        arguments[0],
                        arguments[1],
                        obj,
                        true
                    );
                }
            }

            return obj;
        }

        public T? RegisterSerializer<T>() => (T?)RegisterSerializer(typeof(T));

        public object? RegisterSerializer(Type type)
        {
            if (!type.IsGenericTypeDefinition)
                return RegisterSerializer(type, _ser.CreateSerializer(type));

            var typeArguments = type.GetGenericArguments();
            foreach (var @interface in type.GetInterfaces())
            {
                foreach (var typeInterface in _typeInterfaces)
                {
                    if (!@interface.GetGenericTypeDefinition().HasSameMetadataDefinitionAs(typeInterface))
                        continue;

                    var arguments = @interface.GetGenericArguments();
                    if (arguments.Length != 1)
                        throw new InvalidGenericParameterCountException();
                    var objArguments = arguments[0].GetGenericArguments();
                    for (var i = 0; i < typeArguments.Length; i++)
                    {
                        if (typeArguments[i] != objArguments[i])
                            throw new GenericParameterMismatchException();
                    }

                    _genericTypeSerializers.GetOrNew(typeInterface).TryAdd(arguments[0], type);
                }

                foreach (var typeInterface in _typeNodeInterfaces)
                {
                    if (!@interface.GetGenericTypeDefinition().HasSameMetadataDefinitionAs(typeInterface))
                        continue;

                    var arguments = @interface.GetGenericArguments();
                    if (arguments.Length != 2)
                        throw new InvalidGenericParameterCountException();
                    var objArguments = arguments[0].GetGenericArguments();
                    for (var i = 0; i < typeArguments.Length; i++)
                    {
                        if (typeArguments[i] != objArguments[i])
                            throw new GenericParameterMismatchException();
                    }

                    _genericTypeNodeSerializers.GetOrNew(typeInterface).TryAdd((arguments[0], arguments[1]), type);
                }
            }

            return null;
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
                        for (var i = 0; i < genericParams.Length; i++)
                        {
                            if (genericParams[i].Name != genericInterfaceParams[i].Name)
                                throw new GenericParameterMismatchException();
                        }

                        _typeNodeInterfaces.Add(type);
                    }
                    else if (genericInterface.HasSameMetadataDefinitionAs(genericType))
                    {
                        var genericInterfaceParams = genericInterface.GetGenericArguments();
                        for (var i = 0; i < genericParams.Length; i++)
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
            if (id >= _typeSerializersArray.Length)
                Array.Resize(ref _typeSerializersArray, (id + 1) * 2);

            var array = _typeSerializersArray[id];
            if (array == null)
            {
                array = new (object? Regular, object? Generic)[SerializerInterfaces.Length];
                _typeSerializersArray[id] = array;
            }

            if (regular)
                array[interfaceIndex].Regular = serializer;
            else
                array[interfaceIndex].Generic = serializer;
        }

        private void RegisterIndexedNodeSerializer(Type interfaceIndex, Type elementType, Type nodeType, object serializer, bool regular)
        {
            lock (_lock)
            {
                var id = TypeSerializerType.GetId(interfaceIndex, elementType, nodeType);
                if (id >= _typeNodeSerializersArray.Length)
                    Array.Resize(ref _typeNodeSerializersArray, (id + 1) * 2);

                ref var tuple = ref _typeNodeSerializersArray[id];
                if (regular)
                    tuple.Regular = serializer;
                else
                    tuple.Generic = serializer;

                tuple.Init = true;
            }
        }

        #endregion
    }

    private static class SerializedType
    {
        internal static int Id;
        private static readonly Lock Lock = new();

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

    internal static class SerializedType<T>
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

    internal readonly struct TypeInformation
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

    internal static class TypeSerializerType
    {
        internal static int GetId(Type typeInterface, Type type, Type typeNode)
        {
            var interfaceIndex = SerializerInterfaces.IndexOf(typeInterface.GetGenericTypeDefinition());
            if (interfaceIndex == -1)
                throw new ArgumentException($"Invalid type interface: {typeInterface}");

            var nodeIndex = Nodes.IndexOf(typeNode);
            if (nodeIndex == -1)
                throw new ArgumentException($"Invalid node type: {typeInterface}");

            return SerializedType.GetId(type) *
                   (SerializerInterfaces.Length + Nodes.Length) +
                   interfaceIndex +
                   nodeIndex;
        }
    }

    internal static class TypeSerializerType<TInterface, TType, TNode>
    {
        // ReSharper disable once StaticMemberInGenericType
        internal static readonly int Index = SerializedType<TType>.Information.Id *
                                             (SerializerInterfaces.Length + Nodes.Length) +
                                             SerializerInterfaces.IndexOf(typeof(TInterface).GetGenericTypeDefinition()) +
                                             Nodes.IndexOf(typeof(TNode));
    }
}
