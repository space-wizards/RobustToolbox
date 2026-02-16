using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Definition;
using Robust.Shared.Serialization.Manager.Exceptions;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;

// Avoid accidentally mixing up overloads.
// ReSharper disable RedundantTypeArgumentsOfMethod

namespace Robust.Shared.Serialization.Manager
{
    public partial class SerializationManager
    {
        private delegate object? ReadBoxingDelegate(
            DataNode node,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null);

        private delegate T ReadGenericDelegate<T>(
            DataNode node,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null,
            ISerializationManager.InstantiationDelegate<T>? instanceProvider = null);

        private MethodInfo _read = default!;
        private readonly ConcurrentDictionary<(Type type, bool notNullableOverride), ReadBoxingDelegate> _readBoxingDelegates = new();
        private readonly ConcurrentDictionary<(Type baseType, Type actualType, Type node, bool notNullableOverride), object> _readGenericBaseDelegates = new();
        private readonly ConcurrentDictionary<(Type value, Type node, bool notNullableOverride), object> _readGenericDelegates = new();
        private readonly ConcurrentDictionary<Type, MethodInfo> _reads = new();

        public T Read<T>(DataNode node, ISerializationContext? context = null, bool skipHook = false, ISerializationManager.InstantiationDelegate<T>? instanceProvider = null, bool notNullableOverride = false)
        {
            return Read<T>(
                node,
                SerializationHookContext.ForSkipHooks(skipHook),
                context,
                instanceProvider,
                notNullableOverride);
        }

        public T[] ReadArray<T>(
            DataNode node,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null,
            ISerializationManager.InstantiationDelegate<T[]>? instanceProvider = null,
            bool notNullableOverride = false)
        {
            var baseType = typeof(T);
            var nullable = baseType.IsNullable();
            var isValueType = baseType.IsValueType;

            T[]? val = null;
            if (instanceProvider != null)
                val ??= [];

            if (context != null)
            {
                switch (node)
                {
                    // TODO actual type for type tag
                    case MappingDataNode mapping:
                    {
                        if (context.SerializerProvider
                            .TryGetTypeNodeSerializerArray<ITypeReader<T[], MappingDataNode>, T, MappingDataNode>(
                                out var serializer))
                        {
                            val = serializer.Read(this, mapping, DependencyCollection, hookCtx, context, instanceProvider);
                            if (notNullableOverride)
                                Debug.Assert(val != null, "Reader call returned null value! Forbidden!");
                        }
                        else
                        {
                            RegularRead();
                        }
                        break;
                    }
                    case SequenceDataNode sequence:
                    {
                        if (context.SerializerProvider
                            .TryGetTypeNodeSerializerArray<ITypeReader<T[], SequenceDataNode>, T, SequenceDataNode>(
                                out var serializer))
                        {
                            val = serializer.Read(this, sequence, DependencyCollection, hookCtx, context, instanceProvider);
                            if (notNullableOverride)
                                Debug.Assert(val != null, "Reader call returned null value! Forbidden!");
                        }
                        else
                        {
                            RegularRead();
                        }
                        break;
                    }
                    case ValueDataNode value:
                    {
                        if (context.SerializerProvider
                            .TryGetTypeNodeSerializerArray<ITypeReader<T[], ValueDataNode>, T, ValueDataNode>(
                                out var serializer))
                        {
                            val = serializer.Read(this, value, DependencyCollection, hookCtx, context, instanceProvider);
                            if (notNullableOverride)
                                Debug.Assert(val != null, "Reader call returned null value! Forbidden!");
                        }
                        else
                        {
                            RegularRead();
                        }
                        break;
                    }
                }
            }

            //wrap our valuetype in nullable<T> if we are nullable so we can assign it to returnValue
            // if (nullable && isValueType)

            if (node.IsNull)
            {
                if (nullable && !notNullableOverride)
                    val = null;
                else if (baseType == typeof(EntityUid))
                    val = null;
                else
                    throw new NullNotAllowedException();
            }

            if (!nullable && !isValueType && val == null)
                throw new ReadCallReturnedNullException();

            return val!;

            void RegularRead()
            {
                var hasSerializer = false;
                switch (node)
                {
                    case MappingDataNode mapping:
                    {
                        if (_regularSerializerProvider.TryGetTypeNodeSerializerArray<ITypeReader<T[], MappingDataNode>, T, MappingDataNode>(out var reader))
                        {
                            val = Read<T[], MappingDataNode>(reader, mapping, hookCtx, context, instanceProvider, notNullableOverride);
                        }
                        break;
                    }
                    case SequenceDataNode sequence:
                    {
                        if (_regularSerializerProvider.TryGetTypeNodeSerializerArray<ITypeReader<T[], SequenceDataNode>, T, SequenceDataNode>(out var reader))
                        {
                            val = Read<T[], SequenceDataNode>(reader, sequence, hookCtx, context, instanceProvider, notNullableOverride);
                        }
                        break;
                    }
                    case ValueDataNode value:
                    {
                        if (_regularSerializerProvider.TryGetTypeNodeSerializerArray<ITypeReader<T[], ValueDataNode>, T, ValueDataNode>(out var reader))
                        {
                            val = Read<T[], ValueDataNode>(reader, value, hookCtx, context, instanceProvider, notNullableOverride);
                        }
                        break;
                    }
                }

                if (!baseType.IsArray)
                    return;

                switch (node)
                {
                    case SequenceDataNode sequence:
                    {
                        val = ReadArraySequence<T>(sequence, hookCtx, context);
                        break;
                    }
                    case ValueDataNode value:
                    {
                        val = ReadArrayValue<T>(value, hookCtx, context);
                        break;
                    }
                    default:
                        throw new ArgumentException($"Cannot read array from data node type {node.GetType()}");
                }
            }
        }

        public T ReadStruct<T>(
            T val,
            DataNode node,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null,
            ISerializationManager.InstantiationDelegate<T>? instanceProvider = null,
            bool notNullableOverride = false)
            where T : struct, ISerializationGenerated<T>
        {
            if (node.Tag?.StartsWith("!type:") ?? false)
            {
                var type = ResolveConcreteType(typeof(T), node.Tag.Substring(6));
                if (type.IsInterface || type.IsAbstract)
                {
                    throw new ArgumentException($"Interface or abstract type used for !type node. Type: {type}");
                }

                //!type tag overrides null value on default. i did this because i couldnt come up with a usecase where you'd specify the type but have a null value. yell at me if you found one -paul
                if (node.IsEmpty || node.IsNull)
                {
                    if (instanceProvider != null)
                    {
                        var instantiatedVal = instanceProvider();
                        //make this debug-only? -<paul
                        if (instantiatedVal.GetType() != type) throw new InvalidInstanceReturnedException(type, instantiatedVal.GetType());
                        return instantiatedVal;
                    }

                    return GetOrCreateInstantiator<T>(false, type)();
                }

                return ((ReadGenericDelegate<T>)_readGenericBaseDelegates.GetOrAdd(
                    (typeof(T), type, node.GetType()!, notNullableOverride),
                    static (tuple, manager) => ReadDelegateValueFactory(tuple.baseType, tuple.actualType, tuple.node, tuple.notNullableOverride, manager),
                    this))(node, hookCtx, context, instanceProvider);
            }

            var baseType = typeof(T);
            var nullable = baseType.IsNullable();

            val = default!;

            if (instanceProvider != null)
                val = instanceProvider.Invoke();

            if (context != null)
            {
                switch (node)
                {
                    // TODO actual type for type tag
                    case MappingDataNode mapping:
                    {
                        if (context.SerializerProvider
                            .TryGetTypeNodeSerializer<ITypeReader<T, MappingDataNode>, T, MappingDataNode>(
                                out var serializer))
                        {
                            val = Read<T, MappingDataNode>(
                                serializer,
                                mapping,
                                hookCtx,
                                context,
                                instanceProvider,
                                notNullableOverride
                            );
                        }
                        else
                        {
                            RegularRead();
                        }
                        break;
                    }
                    case SequenceDataNode sequence:
                    {
                        if (context.SerializerProvider
                            .TryGetTypeNodeSerializer<ITypeReader<T, SequenceDataNode>, T, SequenceDataNode>(
                                out var serializer))
                        {
                            val = Read<T, SequenceDataNode>(
                                serializer,
                                sequence,
                                hookCtx,
                                context,
                                instanceProvider,
                                notNullableOverride
                            );
                        }
                        else
                        {
                            RegularRead();
                        }
                        break;
                    }
                    case ValueDataNode value:
                    {
                        if (context.SerializerProvider
                            .TryGetTypeNodeSerializer<ITypeReader<T, ValueDataNode>, T, ValueDataNode>(
                                out var serializer))
                        {
                            val = Read<T, ValueDataNode>(
                                serializer,
                                value,
                                hookCtx,
                                context,
                                instanceProvider,
                                notNullableOverride
                            );
                        }
                        else
                        {
                            RegularRead();
                        }
                        break;
                    }
                }
            }

            //wrap our valuetype in nullable<T> if we are nullable so we can assign it to returnValue
            // if (nullable && isValueType)

            if (node.IsNull)
            {
                if (nullable && !notNullableOverride)
                    val = default!;
                else if (baseType == typeof(EntityUid))
                    val = default!;
                else
                    throw new NullNotAllowedException();
            }

            return val;

            void RegularRead()
            {
                var hasSerializer = false;
                switch (node)
                {
                    case MappingDataNode mapping:
                    {
                        if (_regularSerializerProvider.TryGetTypeNodeSerializer<ITypeReader<T, MappingDataNode>, T, MappingDataNode>(out var reader))
                        {
                            hasSerializer = true;
                            val = Read<T, MappingDataNode>(reader, mapping, hookCtx, context, instanceProvider, notNullableOverride);
                        }
                        break;
                    }
                    case SequenceDataNode sequence:
                    {
                        if (_regularSerializerProvider.TryGetTypeNodeSerializer<ITypeReader<T, SequenceDataNode>, T, SequenceDataNode>(out var reader))
                        {
                            hasSerializer = true;
                            val = Read<T, SequenceDataNode>(reader, sequence, hookCtx, context, instanceProvider, notNullableOverride);
                        }
                        break;
                    }
                    case ValueDataNode value:
                    {
                        if (_regularSerializerProvider.TryGetTypeNodeSerializer<ITypeReader<T, ValueDataNode>, T, ValueDataNode>(out var reader))
                        {
                            hasSerializer = true;
                            val = Read<T, ValueDataNode>(reader, value, hookCtx, context, instanceProvider, notNullableOverride);
                        }
                        break;
                    }
                }

                if (!hasSerializer)
                {
                    if (baseType.IsArray)
                    {
                        switch (node)
                        {
                            case SequenceDataNode sequence:
                            {
                                val = (T) (object) ReadArraySequence<T>(sequence, hookCtx, context);
                                break;
                            }
                            case ValueDataNode value:
                            {
                                val = (T) (object) ReadArrayValue<T>(value, hookCtx, context);
                                break;
                            }
                            default:
                                throw new ArgumentException($"Cannot read array from data node type {node.GetType()}");
                        }
                    }
                    else if (baseType.IsEnum)
                    {
                        switch (node)
                        {
                            case SequenceDataNode sequence:
                            {
                                val = ReadEnumSequence<T>(sequence);
                                break;
                            }
                            case ValueDataNode value:
                            {
                                val = ReadEnumValue<T>(value);
                                break;
                            }
                            default:
                                throw new InvalidNodeTypeException($"Cannot serialize node as {baseType}, unsupported node type {node.GetType()}");
                        }
                    }
                    else if (baseType.IsAssignableTo(typeof(ISelfSerialize)))
                    {
                        switch (node)
                        {
                            case ValueDataNode value:
                            {
                                instanceProvider ??= GetOrCreateInstantiator<T>(false, baseType);
                                val = instanceProvider();
                                var selfSerialize = (ISelfSerialize) val;
                                selfSerialize.Deserialize(value.Value);
                                val = (T) selfSerialize;
                                break;
                            }
                            default:
                                throw new InvalidNodeTypeException($"Cannot read {nameof(ISelfSerialize)} from node type {node.GetType()}. Expected {nameof(ValueDataNode)}");
                        }
                    }
                    else
                    {
                        switch (node)
                        {
                            case MappingDataNode mapping:
                            {
                                instanceProvider ??= GetOrCreateInstantiator<T>(false);
                                var definition = GetDefinition<T>();
                                val = ReadGenericMapping<T>(mapping, definition, hookCtx, context, instanceProvider);
                                break;
                            }
                            case ValueDataNode value:
                            {
                                instanceProvider ??= GetOrCreateInstantiator<T>(false);
                                val = ReadGenericValue<T>(value, hookCtx, instanceProvider);
                                break;
                            }
                            default:
                                throw new ArgumentException($"No mapping or value node provided for type {baseType}.");
                        }
                    }
                }
            }
        }

        public T ReadEnum<T>(
            T val,
            DataNode node,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null,
            ISerializationManager.InstantiationDelegate<T>? instanceProvider = null,
            bool notNullableOverride = false)
            where T : struct, Enum
        {
            if (node.Tag?.StartsWith("!type:") ?? false)
            {
                var type = ResolveConcreteType(typeof(T), node.Tag.Substring(6));
                if (type.IsInterface || type.IsAbstract)
                {
                    throw new ArgumentException($"Interface or abstract type used for !type node. Type: {type}");
                }

                //!type tag overrides null value on default. i did this because i couldnt come up with a usecase where you'd specify the type but have a null value. yell at me if you found one -paul
                if (node.IsEmpty || node.IsNull)
                {
                    if (instanceProvider != null)
                    {
                        var instantiatedVal = instanceProvider();
                        //make this debug-only? -<paul
                        if (instantiatedVal.GetType() != type) throw new InvalidInstanceReturnedException(type, instantiatedVal.GetType());
                        return instantiatedVal;
                    }

                    return GetOrCreateInstantiator<T>(false, type)();
                }

                return ((ReadGenericDelegate<T>)_readGenericBaseDelegates.GetOrAdd(
                    (typeof(T), type, node.GetType()!, notNullableOverride),
                    static (tuple, manager) => ReadDelegateValueFactory(tuple.baseType, tuple.actualType, tuple.node, tuple.notNullableOverride, manager),
                    this))(node, hookCtx, context, instanceProvider);
            }

            var baseType = typeof(T);
            var nullable = baseType.IsNullable();

            val = default!;

            if (instanceProvider != null)
                val = instanceProvider.Invoke();

            if (context != null)
            {
                switch (node)
                {
                    // TODO actual type for type tag
                    case MappingDataNode mapping:
                    {
                        if (context.SerializerProvider
                            .TryGetTypeNodeSerializer<ITypeReader<T, MappingDataNode>, T, MappingDataNode>(
                                out var serializer))
                        {
                            val = Read<T, MappingDataNode>(
                                serializer,
                                mapping,
                                hookCtx,
                                context,
                                instanceProvider,
                                notNullableOverride
                            );
                        }
                        else
                        {
                            RegularRead();
                        }
                        break;
                    }
                    case SequenceDataNode sequence:
                    {
                        if (context.SerializerProvider
                            .TryGetTypeNodeSerializer<ITypeReader<T, SequenceDataNode>, T, SequenceDataNode>(
                                out var serializer))
                        {
                            val = Read<T, SequenceDataNode>(
                                serializer,
                                sequence,
                                hookCtx,
                                context,
                                instanceProvider,
                                notNullableOverride
                            );
                        }
                        else
                        {
                            RegularRead();
                        }
                        break;
                    }
                    case ValueDataNode value:
                    {
                        if (context.SerializerProvider
                            .TryGetTypeNodeSerializer<ITypeReader<T, ValueDataNode>, T, ValueDataNode>(
                                out var serializer))
                        {
                            val = Read<T, ValueDataNode>(
                                serializer,
                                value,
                                hookCtx,
                                context,
                                instanceProvider,
                                notNullableOverride
                            );
                        }
                        else
                        {
                            RegularRead();
                        }
                        break;
                    }
                }
            }

            //wrap our valuetype in nullable<T> if we are nullable so we can assign it to returnValue
            // if (nullable && isValueType)

            if (node.IsNull)
            {
                if (nullable && !notNullableOverride)
                    val = default!;
                else if (baseType == typeof(EntityUid))
                    val = default!;
                else
                    throw new NullNotAllowedException();
            }

            return val;

            void RegularRead()
            {
                var hasSerializer = false;
                switch (node)
                {
                    case MappingDataNode mapping:
                    {
                        if (_regularSerializerProvider.TryGetTypeNodeSerializer<ITypeReader<T, MappingDataNode>, T, MappingDataNode>(out var reader))
                        {
                            hasSerializer = true;
                            val = Read<T, MappingDataNode>(reader, mapping, hookCtx, context, instanceProvider, notNullableOverride);
                        }
                        break;
                    }
                    case SequenceDataNode sequence:
                    {
                        if (_regularSerializerProvider.TryGetTypeNodeSerializer<ITypeReader<T, SequenceDataNode>, T, SequenceDataNode>(out var reader))
                        {
                            hasSerializer = true;
                            val = Read<T, SequenceDataNode>(reader, sequence, hookCtx, context, instanceProvider, notNullableOverride);
                        }
                        break;
                    }
                    case ValueDataNode value:
                    {
                        if (_regularSerializerProvider.TryGetTypeNodeSerializer<ITypeReader<T, ValueDataNode>, T, ValueDataNode>(out var reader))
                        {
                            hasSerializer = true;
                            val = Read<T, ValueDataNode>(reader, value, hookCtx, context, instanceProvider, notNullableOverride);
                        }
                        break;
                    }
                }

                if (hasSerializer)
                    return;

                switch (node)
                {
                    case SequenceDataNode sequence:
                    {
                        val = ReadEnumSequence<T>(sequence);
                        break;
                    }
                    case ValueDataNode value:
                    {
                        val = ReadEnumValue<T>(value);
                        break;
                    }
                    default:
                        throw new InvalidNodeTypeException($"Cannot serialize node as {baseType}, unsupported node type {node.GetType()}");
                }
            }
        }

        public T Read<T>(
            DataNode node,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null,
            ISerializationManager.InstantiationDelegate<T>? instanceProvider = null,
            bool notNullableOverride = false)
        {
            if (node.Tag?.StartsWith("!type:") ?? false)
            {
                var type = ResolveConcreteType(typeof(T), node.Tag.Substring(6));
                if (type.IsInterface || type.IsAbstract)
                {
                    throw new ArgumentException($"Interface or abstract type used for !type node. Type: {type}");
                }

                //!type tag overrides null value on default. i did this because i couldnt come up with a usecase where you'd specify the type but have a null value. yell at me if you found one -paul
                if (node.IsEmpty || node.IsNull)
                {
                    if (instanceProvider != null)
                    {
                        var instantiatedVal = instanceProvider();
                        //make this debug-only? -<paul
                        if (instantiatedVal?.GetType() != type) throw new InvalidInstanceReturnedException(type, instantiatedVal?.GetType());
                        return instantiatedVal;
                    }

                    return GetOrCreateInstantiator<T>(false, type)();
                }

                return ((ReadGenericDelegate<T>)_readGenericBaseDelegates.GetOrAdd(
                    (typeof(T), type, node.GetType()!, notNullableOverride),
                    static (tuple, manager) => ReadDelegateValueFactory(tuple.baseType, tuple.actualType, tuple.node, tuple.notNullableOverride, manager),
                    this))(node, hookCtx, context, instanceProvider);
            }

            var baseType = typeof(T);
            if (baseType.IsEnum || baseType.IsArray ||
                (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(Nullable<>)))
            {
                return ((ReadGenericDelegate<T>)_readGenericDelegates.GetOrAdd((typeof(T), node.GetType()!, notNullableOverride),
                    static (tuple, manager) => ReadDelegateValueFactory(tuple.value, tuple.value, tuple.node, tuple.notNullableOverride, manager), this))(node, hookCtx, context, instanceProvider);
            }

            var nullable = baseType.IsNullable();

            T val = default!;
            if (node.IsNull)
            {
                if (nullable && !notNullableOverride)
                    return default!;

                if (baseType == typeof(EntityUid))
                    return (T) (object) EntityUid.Invalid;

                throw new NullNotAllowedException();
            }

            if (instanceProvider != null)
                val = instanceProvider.Invoke();

            if (context != null)
            {
                switch (node)
                {
                    // TODO actual type for type tag
                    case MappingDataNode mapping:
                    {
                        if (context.SerializerProvider
                            .TryGetTypeNodeSerializer<ITypeReader<T, MappingDataNode>, T, MappingDataNode>(
                                out var serializer))
                        {
                            val = Read<T, MappingDataNode>(
                                serializer,
                                mapping,
                                hookCtx,
                                context,
                                instanceProvider,
                                notNullableOverride
                            );
                        }
                        else
                        {
                            RegularRead();
                        }

                        break;
                    }
                    case SequenceDataNode sequence:
                    {
                        if (context.SerializerProvider
                            .TryGetTypeNodeSerializer<ITypeReader<T, SequenceDataNode>, T, SequenceDataNode>(
                                out var serializer))
                        {
                            val = Read<T, SequenceDataNode>(
                                serializer,
                                sequence,
                                hookCtx,
                                context,
                                instanceProvider,
                                notNullableOverride
                            );
                        }
                        else
                        {
                            RegularRead();
                        }

                        break;
                    }
                    case ValueDataNode value:
                    {
                        if (context.SerializerProvider
                            .TryGetTypeNodeSerializer<ITypeReader<T, ValueDataNode>, T, ValueDataNode>(
                                out var serializer))
                        {
                            val = Read<T, ValueDataNode>(
                                serializer,
                                value,
                                hookCtx,
                                context,
                                instanceProvider,
                                notNullableOverride
                            );
                        }
                        else
                        {
                            RegularRead();
                        }

                        break;
                    }
                }
            }
            else
            {
                RegularRead();
            }

            //wrap our valuetype in nullable<T> if we are nullable so we can assign it to returnValue
            // if (nullable && isValueType)

            var isValueType = baseType.IsValueType;
            if (!nullable && !isValueType && val == null)
                throw new ReadCallReturnedNullException();

            return val;

            void RegularRead()
            {
                var hasSerializer = false;
                switch (node)
                {
                    case MappingDataNode mapping:
                    {
                        if (_regularSerializerProvider.TryGetTypeNodeSerializer<ITypeReader<T, MappingDataNode>, T, MappingDataNode>(out var reader))
                        {
                            hasSerializer = true;
                            val = Read<T, MappingDataNode>(reader, mapping, hookCtx, context, instanceProvider, notNullableOverride);
                        }
                        break;
                    }
                    case SequenceDataNode sequence:
                    {
                        if (_regularSerializerProvider.TryGetTypeNodeSerializer<ITypeReader<T, SequenceDataNode>, T, SequenceDataNode>(out var reader))
                        {
                            hasSerializer = true;
                            val = Read<T, SequenceDataNode>(reader, sequence, hookCtx, context, instanceProvider, notNullableOverride);
                        }
                        break;
                    }
                    case ValueDataNode value:
                    {
                        if (_regularSerializerProvider.TryGetTypeNodeSerializer<ITypeReader<T, ValueDataNode>, T, ValueDataNode>(out var reader))
                        {
                            hasSerializer = true;
                            val = Read<T, ValueDataNode>(reader, value, hookCtx, context, instanceProvider, notNullableOverride);
                        }
                        break;
                    }
                }

                if (!hasSerializer)
                {
                    if (baseType.IsAssignableTo(typeof(ISelfSerialize)))
                    {
                        switch (node)
                        {
                            case ValueDataNode value:
                            {
                                instanceProvider ??= GetOrCreateInstantiator<T>(false);
                                val = instanceProvider();
                                var selfSerialize = (ISelfSerialize?) val;
                                selfSerialize!.Deserialize(value.Value);
                                val = (T) selfSerialize;
                                break;
                            }
                            default:
                                throw new InvalidNodeTypeException($"Cannot read {nameof(ISelfSerialize)} from node type {node.GetType()}. Expected {nameof(ValueDataNode)}");
                        }
                    }
                    else
                    {
                        switch (node)
                        {
                            case MappingDataNode mapping:
                            {
                                instanceProvider ??= GetOrCreateInstantiator<T>(false);
                                var definition = GetDefinition(typeof(T));
                                if (definition == null)
                                {
                                    throw new ArgumentException($"No data definition found for type {baseType} with node type {node.GetType()} when reading");
                                }

                                val = instanceProvider();

                                var valObj = (object) val!;
                                definition.PopulateObj(ref valObj, mapping, this, hookCtx, context);
                                val = (T)valObj;

                                RunAfterHook(val, hookCtx);
                                break;
                            }
                            case ValueDataNode value:
                            {
                                instanceProvider ??= GetOrCreateInstantiator<T>(false);
                                val = instanceProvider();
                                if (value.Value != string.Empty)
                                {
                                    throw new ArgumentException($"No mapping node provided for type {baseType} at line: {node.Start.Line}");
                                }

                                RunAfterHook(val, hookCtx);
                                break;
                            }
                            default:
                                throw new ArgumentException($"No mapping or value node provided for type {baseType}.");
                        }
                    }
                }
            }
        }

        public T Read<T, TNode>(ITypeReader<T, TNode> reader, TNode node, ISerializationContext? context = null,
            bool skipHook = false, ISerializationManager.InstantiationDelegate<T>? instanceProvider = null, bool notNullableOverride = false)
            where TNode : DataNode
        {
            return Read<T, TNode>(
                reader,
                node,
                SerializationHookContext.ForSkipHooks(skipHook),
                context,
                instanceProvider,
                notNullableOverride);
        }

        public T Read<T, TNode>(
            ITypeReader<T, TNode> reader,
            TNode node,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null,
            ISerializationManager.InstantiationDelegate<T>? instanceProvider = null,
            bool notNullableOverride = false)
            where TNode : DataNode
        {
            var val = reader.Read(this, node, DependencyCollection, hookCtx, context, instanceProvider);
            if (notNullableOverride)
                Debug.Assert(val != null, "Reader call returned null value! Forbidden!");

            return val;
        }

        public T Read<T, TNode, TReader>(TNode node, ISerializationContext? context = null,
            bool skipHook = false, ISerializationManager.InstantiationDelegate<T>? instanceProvider = null, bool notNullableOverride = false) where TNode : DataNode
            where TReader : ITypeReader<T, TNode>
        {
            return Read<T, TNode, TReader>(
                node,
                SerializationHookContext.ForSkipHooks(skipHook),
                context,
                instanceProvider,
                notNullableOverride);
        }

        public T Read<T, TNode, TReader>(
            TNode node,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null,
            ISerializationManager.InstantiationDelegate<T>? instanceProvider = null,
            bool notNullableOverride = false)
            where TNode : DataNode
            where TReader : ITypeReader<T, TNode>
        {
            return Read(
                GetOrCreateCustomTypeSerializer<TReader>(),
                node,
                hookCtx,
                context,
                instanceProvider,
                notNullableOverride);
        }

        public object? Read(Type type, DataNode node, ISerializationContext? context = null, bool skipHook = false, bool notNullableOverride = false)
        {
            return Read(type, node, SerializationHookContext.ForSkipHooks(skipHook), context, notNullableOverride);
        }

        public object? Read(
            Type type,
            DataNode node,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null,
            bool notNullableOverride = false)
        {
            var read = _reads.GetOrAdd(type, t => _read.MakeGenericMethod(t));
            return read.Invoke(this, [node, hookCtx, context, null, notNullableOverride]);
        }

        private ReadBoxingDelegate GetOrCreateBoxingReadDelegate(Type type, bool notNullableOverride = false)
        {
            return _readBoxingDelegates.GetOrAdd((type, notNullableOverride), static (tuple, manager) =>
            {
                var type = tuple.type;
                var managerConst = Expression.Constant(manager);

                var nodeParam = Expression.Variable(typeof(DataNode));
                var contextParam = Expression.Variable(typeof(ISerializationContext));
                var hookCtxParam = Expression.Variable(typeof(SerializationHookContext));

                var call = Expression.Convert(Expression.Call(
                    managerConst,
                    nameof(Read),
                    new[] { type },
                    nodeParam,
                    hookCtxParam,
                    contextParam,
                    Expression.Constant(null, typeof(ISerializationManager.InstantiationDelegate<>).MakeGenericType(type)),
                    Expression.Constant(tuple.notNullableOverride)), typeof(object));

                return Expression.Lambda<ReadBoxingDelegate>(
                    call,
                    nodeParam,
                    hookCtxParam,
                    contextParam).Compile();
            }, this);
        }

        private static object ReadDelegateValueFactory(Type baseType, Type actualType, Type nodeType, bool notNullableOverride, SerializationManager manager)
        {
            var nullable = actualType.IsNullable();

            var managerConst = Expression.Constant(manager);

            var nodeParam = Expression.Parameter(typeof(DataNode), "node");
            var contextParam = Expression.Parameter(typeof(ISerializationContext), "context");
            var hookCtxParam = Expression.Parameter(typeof(SerializationHookContext), "hookCtx");
            var instantiatorParam = Expression.Parameter(typeof(ISerializationManager.InstantiationDelegate<>).MakeGenericType(baseType), "instanceProvider");

            actualType = actualType.EnsureNotNullableType();

            Expression BaseInstantiatorToActual()
            {
                Expression nonNullableInstantiator = baseType.IsNullable() && baseType.IsValueType
                    ? Expression.Call(
                        managerConst,
                        nameof(UnwrapInstantiationDelegate),
                        new[] { baseType.EnsureNotNullableType() },
                        instantiatorParam)
                    : instantiatorParam;

                return baseType.EnsureNotNullableType() == actualType
                    ? nonNullableInstantiator
                    : Expression.Call(managerConst,
                        nameof(WrapBaseInstantiationDelegate),
                        new []{actualType, baseType.EnsureNotNullableType()},
                        instantiatorParam);
            }

            var instantiatorVariable =
                Expression.Variable(typeof(ISerializationManager.InstantiationDelegate<>).MakeGenericType(actualType));

            var instantiatorCoalesce = Expression.Assign(instantiatorVariable, Expression.Coalesce(
                BaseInstantiatorToActual(),
                Expression.Call(
                    managerConst,
                    nameof(GetOrCreateInstantiator),
                    new[] { actualType },
                    Expression.Constant(false),
                    Expression.Constant(null, typeof(Type)))));

            Expression call;
            if (manager._regularSerializerProvider.TryGetTypeNodeSerializer(typeof(ITypeReader<,>), actualType, nodeType, out var reader))
            {
                var readerType = typeof(ITypeReader<,>).MakeGenericType(actualType, nodeType);
                var readerConst = Expression.Constant(reader, readerType);

                call = Expression.Call(
                    managerConst,
                    nameof(Read),
                    new[] { actualType, nodeType },
                    readerConst,
                    Expression.Convert(nodeParam, nodeType),
                    hookCtxParam,
                    contextParam,
                    BaseInstantiatorToActual(),
                    Expression.Constant(notNullableOverride));
            }
            else if (actualType.IsArray)
            {
                var elementType = actualType.GetElementType()!;

                if (nodeType == typeof(ValueDataNode))
                {
                    call = Expression.Call(
                        managerConst,
                        nameof(ReadArrayValue),
                        new[] { elementType },
                        Expression.Convert(nodeParam, typeof(ValueDataNode)),
                        hookCtxParam,
                        contextParam);
                }
                else if (nodeType == typeof(SequenceDataNode))
                {
                    call = Expression.Call(
                        managerConst,
                        nameof(ReadArraySequence),
                        new[] { elementType },
                        Expression.Convert(nodeParam, typeof(SequenceDataNode)),
                        hookCtxParam,
                        contextParam);
                }
                else
                {
                    throw new ArgumentException($"Cannot read array from data node type {nodeType}");
                }
            }
            else if (actualType.IsEnum)
            {
                // Does not include cases where the target type is System.Enum.
                // Those get handled by the generic enum serializer which uses reflection to resolve strings into enums.
                DebugTools.Assert(actualType != typeof(Enum));

                if (nodeType == typeof(ValueDataNode))
                {
                    call = Expression.Call(managerConst, nameof(ReadEnumValue), new[] { actualType },
                        Expression.Convert(nodeParam, typeof(ValueDataNode)));
                }
                else if (nodeType == typeof(SequenceDataNode))
                {
                    call = Expression.Call(managerConst, nameof(ReadEnumSequence), new[] { actualType },
                        Expression.Convert(nodeParam, typeof(SequenceDataNode)));
                }
                else
                {
                    throw new InvalidNodeTypeException($"Cannot serialize node as {actualType}, unsupported node type {nodeType}");
                }
            }
            else if (actualType.IsAssignableTo(typeof(ISelfSerialize)))
            {
                if (nodeType != typeof(ValueDataNode))
                {
                    throw new InvalidNodeTypeException($"Cannot read {nameof(ISelfSerialize)} from node type {nodeType}. Expected {nameof(ValueDataNode)}");
                }

                call = Expression.Block(
                    new [] {instantiatorVariable},
                    instantiatorCoalesce,
                    Expression.Call(
                        managerConst,
                        nameof(ReadSelfSerialize),
                        new[] { actualType },
                        instantiatorVariable,
                        Expression.Convert(nodeParam, typeof(ValueDataNode))));
            }
            else
            {
                if (nodeType == typeof(ValueDataNode))
                {
                    call = Expression.Call(
                        managerConst,
                        nameof(ReadGenericValue),
                        new[] { actualType },
                        Expression.Convert(nodeParam, typeof(ValueDataNode)),
                        hookCtxParam,
                        instantiatorVariable);
                }
                else if (nodeType == typeof(MappingDataNode))
                {
                    var definition = manager.GetDefinition(actualType);
                    var definitionConst = Expression.Constant(definition, typeof(DataDefinition<>).MakeGenericType(actualType));

                    call = Expression.Call(
                        managerConst,
                        nameof(ReadGenericMapping),
                        new[] { actualType },
                        Expression.Convert(nodeParam, typeof(MappingDataNode)),
                        definitionConst,
                        hookCtxParam,
                        contextParam,
                        instantiatorVariable);
                }
                else
                {
                    throw new ArgumentException($"No mapping or value node provided for type {actualType}.");
                }

                call = Expression.Block(
                    new[]{instantiatorVariable},
                    instantiatorCoalesce,
                    call);
            }

            // check for customtypeserializer
            var serializerType = typeof(ITypeReader<,>).MakeGenericType(actualType, nodeType);
            var serializerVar = Expression.Variable(serializerType);
            call = Expression.Block(new[] { serializerVar },
                Expression.Condition(
                    Expression.AndAlso(
                        Expression.ReferenceNotEqual(contextParam,
                            Expression.Constant(null, typeof(ISerializationContext))),
                        Expression.Call(Expression.Property(contextParam, "SerializerProvider"),
                            "TryGetTypeNodeSerializer", new[] { serializerType, actualType, nodeType }, serializerVar)),
                    Expression.Call(
                            managerConst,
                            nameof(Read),
                            new []{actualType, nodeType},
                            serializerVar,
                            Expression.Convert(nodeParam, nodeType),
                            hookCtxParam,
                            contextParam,
                            BaseInstantiatorToActual(),
                            Expression.Constant(notNullableOverride)),
                    call));

            //wrap our valuetype in nullable<T> if we are nullable so we can assign it to returnValue
            call = WrapNullableIfNeededExpression(call, nullable);

            // early-out null before anything
            var returnValue = Expression.Variable(nullable ? actualType.EnsureNullableType() : actualType);
            call = Expression.Block(new[] { returnValue },
                Expression.IfThenElse(
                    Expression.Call(typeof(SerializationManager), nameof(IsNull), Type.EmptyTypes, nodeParam),
                    nullable && !notNullableOverride
                        ? Expression.Block(typeof(void),
                            Expression.Assign(returnValue, GetNullExpression(managerConst, actualType)))
                        : actualType == typeof(EntityUid) //todo paul make this not hardcoded
                            ? Expression.Assign(returnValue, Expression.Constant(EntityUid.Invalid))
                            : ExpressionUtils.ThrowExpression<NullNotAllowedException>(),
                Expression.Block(typeof(void),
                    Expression.Assign(returnValue, call))),
                returnValue);

            if (!nullable && !actualType.IsValueType)
            {
                // check that value isn't null
                var finalValue = Expression.Variable(baseType);
                call = Expression.Block(new[] { finalValue },
                    Expression.Assign(finalValue, call),
                    Expression.IfThen(Expression.Equal(finalValue, GetNullExpression(managerConst, actualType)),
                        ExpressionUtils.ThrowExpression<ReadCallReturnedNullException>()),
                    finalValue);
            }

            return Expression.Lambda(
                typeof(ReadGenericDelegate<>).MakeGenericType(baseType),
                call,
                nodeParam,
                hookCtxParam,
                contextParam,
                instantiatorParam)
                .Compile();
        }

        private ISerializationManager.InstantiationDelegate<T>? UnwrapInstantiationDelegate<T>(
            ISerializationManager.InstantiationDelegate<T?>? instantiationDelegate) where T : struct
        {
            if (instantiationDelegate == null) return null;

            return () =>
            {
                var val = instantiationDelegate();
                Debug.Assert(val.HasValue, $"{nameof(instantiationDelegate)} returned null value! This should NEVER be allowed to happen!");
                return instantiationDelegate()!.Value;
            };
        }

        private ISerializationManager.InstantiationDelegate<TActual>? WrapBaseInstantiationDelegate<TActual, TBase>(
            ISerializationManager.InstantiationDelegate<TBase>? instantiationDelegate) where TActual : TBase
        {
            if (instantiationDelegate == null) return null;

            return () =>
            {
                var val = instantiationDelegate();
                Debug.Assert(val != null, $"{nameof(instantiationDelegate)} returned null value! This should NEVER be allowed to happen!");
                return (TActual)val;
            };
        }


        private T[] ReadArrayValue<T>(
            ValueDataNode value,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null)
        {
            var array = new T[1];
            array[0] = Read<T>(value, hookCtx, context);
            return array;
        }

        private T[] ReadArraySequence<T>(
            SequenceDataNode node,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null)
        {
            var array = new T[node.Sequence.Count];

            for (var i = 0; i < node.Sequence.Count; i++)
            {
                array[i] = Read<T>(node.Sequence[i], hookCtx, context);
            }

            return array;
        }

        private TEnum ReadEnumValue<TEnum>(ValueDataNode node) where TEnum : struct
        {
            return Enum.Parse<TEnum>(node.Value, true);
        }

        private TEnum ReadEnumSequence<TEnum>(SequenceDataNode node) where TEnum : struct
        {
            return Enum.Parse<TEnum>(string.Join(", ", node.Sequence), true);
        }

        private TValue ReadSelfSerialize<TValue>(
            ISerializationManager.InstantiationDelegate<TValue> instanceProvider, ValueDataNode node) where TValue : ISelfSerialize
        {
            var val = instanceProvider();
            val.Deserialize(node.Value);
            return val;
        }

        private TValue ReadGenericValue<TValue>(
            ValueDataNode node,
            SerializationHookContext hookCtx,
            ISerializationManager.InstantiationDelegate<TValue> instanceProvider)
            where TValue : notnull
        {
            var type = typeof(TValue);
            var instance = instanceProvider();

            if (node.Value != string.Empty)
            {
                throw new ArgumentException($"No mapping node provided for type {type} at line: {node.Start.Line}");
            }

            RunAfterHook(instance, hookCtx);

            return instance;
        }

        private TValue ReadGenericMapping<TValue>(
            MappingDataNode node,
            DataDefinition<TValue>? definition,
            SerializationHookContext hookCtx,
            ISerializationContext? context,
            ISerializationManager.InstantiationDelegate<TValue> instanceProvider)
            where TValue : ISerializationGenerated<TValue>
        {
            if (definition == null)
            {
                throw new ArgumentException($"No data definition found for type {typeof(TValue)} with node type {node.GetType()} when reading");
            }

            var instance = instanceProvider();

            definition.Populate(ref instance, node, this, hookCtx, context);

            RunAfterHook(instance, hookCtx);

            return instance;
        }
    }
}
