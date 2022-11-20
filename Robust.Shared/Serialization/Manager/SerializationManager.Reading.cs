using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Robust.Shared.Serialization.Manager.Definition;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization.Manager
{
    public partial class SerializationManager
    {
        private delegate object? ReadDelegate(
            Type type,
            DataNode node,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null,
            object? value = null);

        private readonly ConcurrentDictionary<(Type value, Type node), ReadDelegate> _readers = new();

        public T Read<T>(DataNode node, ISerializationContext? context = null, bool skipHook = false, T? value = default) //todo paul this default should be null
        {
            return Read<T>(
                node,
                SerializationHookContext.ForSkipHooks(skipHook),
                context,
                value);
        }

        public T Read<T>(DataNode node, SerializationHookContext hookCtx, ISerializationContext? context = null, T? value = default) //todo paul this default should be null
        {
            return (T)Read(
                typeof(T),
                node,
                hookCtx,
                context,
                EqualityComparer<T>.Default.Equals(value, default) ? null : value)!;
        }

        public object? Read(Type type, DataNode node, ISerializationContext? context = null, bool skipHook = false, object? value = null)
        {
            return Read(type, node, SerializationHookContext.ForSkipHooks(skipHook), context, value);
        }

        public object? Read(
            Type type,
            DataNode node,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null,
            object? value = null)
        {
            var val = GetOrCreateReader(type, node)(type, node, hookCtx, context, value);
            ReadNullCheck(type, val);
            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReadNullCheck(Type type, object? val)
        {
            if (!type.IsNullable() && val == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(Read)}-Call returned a null value for non-nullable type {type}");
            }
        }

        private ReadDelegate GetOrCreateReader(Type value, DataNode node)
        {
            if (node.Tag?.StartsWith("!type:") ?? false)
            {
                var typeString = node.Tag.Substring(6);
                value = ResolveConcreteType(value, typeString, _reflectionManager);
            }

            return _readers.GetOrAdd((value, node.GetType()), static (tuple, vfArgument) =>
            {
                var (value, nodeType) = tuple;
                var (node, instance) = vfArgument;

                var nullable = value.IsNullable();
                value = value.EnsureNotNullableType();

                var instanceConst = Expression.Constant(instance);

                var typeParam = Expression.Parameter(typeof(Type), "type");
                var nodeParam = Expression.Parameter(typeof(DataNode), "node");
                //todo paul serializers in the context should also override default serializers for array etc
                var contextParam = Expression.Parameter(typeof(ISerializationContext), "context");
                var hookCtxParam = Expression.Parameter(typeof(SerializationHookContext), "hookCtx");
                var valueParam = Expression.Parameter(typeof(object), "value");

                Expression call;

                if (value.IsArray)
                {
                    var elementType = value.GetElementType()!;

                    switch (node)
                    {
                        case ValueDataNode:
                            call = Expression.Call(
                                instanceConst,
                                nameof(ReadArrayValue),
                                new[] { elementType },
                                Expression.Convert(nodeParam, typeof(ValueDataNode)),
                                hookCtxParam,
                                contextParam);
                            break;
                        case SequenceDataNode seqNode:
                            var isSealed = elementType.IsPrimitive || elementType.IsEnum ||
                                           elementType == typeof(string) || elementType.IsSealed;

                            if (isSealed && seqNode.Sequence.Count > 0)
                            {
                                var reader = instance.GetOrCreateReader(elementType, seqNode.Sequence[0]);
                                var readerConst = Expression.Constant(reader);

                                call = Expression.Call(
                                    instanceConst,
                                    nameof(ReadArraySequenceSealed),
                                    new[] { elementType },
                                    Expression.Convert(nodeParam, typeof(SequenceDataNode)),
                                    readerConst,
                                    hookCtxParam,
                                    contextParam);

                                break;
                            }

                            call = Expression.Call(
                                instanceConst,
                                nameof(ReadArraySequence),
                                new[] { elementType },
                                Expression.Convert(nodeParam, typeof(SequenceDataNode)),
                                hookCtxParam,
                                contextParam);
                            break;
                        default:
                            throw new ArgumentException($"Cannot read array from data node type {nodeType}");
                    }
                }
                else if (value.IsEnum)
                {
                    call = node switch
                    {
                        ValueDataNode => Expression.Call(
                            instanceConst,
                            nameof(ReadEnumValue),
                            new[] { value },
                            Expression.Convert(nodeParam, typeof(ValueDataNode))),
                        SequenceDataNode => Expression.Call(
                            instanceConst,
                            nameof(ReadEnumSequence),
                            new[] { value },
                            Expression.Convert(nodeParam, typeof(SequenceDataNode))),
                        _ => throw new InvalidNodeTypeException(
                            $"Cannot serialize node as {value}, unsupported node type {node.GetType()}")
                    };
                }
                else if (value.IsAssignableTo(typeof(ISelfSerialize)))
                {
                    if (node is not ValueDataNode)
                    {
                        throw new InvalidNodeTypeException(
                            $"Cannot read {nameof(ISelfSerialize)} from node type {nodeType}. Expected {nameof(ValueDataNode)}");
                    }

                    var definition = instance.GetDefinition(value);
                    var instantiator = instance.GetOrCreateInstantiator(value, definition?.IsRecord ?? false);
                    var instantiatorConst = Expression.Constant(instantiator);

                    call = Expression.Call(
                        instanceConst,
                        nameof(ReadSelfSerialize),
                        new[] { value },
                        Expression.Convert(nodeParam, typeof(ValueDataNode)),
                        instantiatorConst, valueParam);
                }
                else if (instance.TryGetTypeReader(value, nodeType, out var reader))
                {
                    var readerType = typeof(ITypeReader<,>).MakeGenericType(value, nodeType);
                    var readerConst = Expression.Constant(reader, readerType);

                    call = Expression.Call(
                        instanceConst,
                        nameof(ReadWithTypeReader),
                        new[] { value, nodeType },
                        Expression.Convert(nodeParam, nodeType),
                        readerConst,
                        hookCtxParam,
                        contextParam,
                        valueParam);
                }
                else if (value.IsInterface || value.IsAbstract)
                {
                    throw new ArgumentException($"Unable to create an instance of an interface or abstract type. Type: {value}");
                }
                else
                {
                    var definition = instance.GetDefinition(value);
                    var definitionConst = Expression.Constant(definition, typeof(DataDefinition));

                    var instantiator = instance.GetOrCreateInstantiator(value, definition?.IsRecord ?? false);
                    var instantiatorConst = Expression.Constant(instantiator);

                    call = node switch
                    {
                        ValueDataNode => Expression.Call(
                            instanceConst,
                            nameof(ReadGenericValue),
                            new[] { value },
                            Expression.Convert(nodeParam, typeof(ValueDataNode)),
                            instantiatorConst,
                            definitionConst,
                            hookCtxParam,
                            contextParam,
                            valueParam),
                        MappingDataNode => Expression.Call(
                            instanceConst,
                            nameof(ReadGenericMapping),
                            new[] { value },
                            Expression.Convert(nodeParam, typeof(MappingDataNode)),
                            instantiatorConst,
                            definitionConst,
                            hookCtxParam,
                            contextParam,
                            valueParam),
                        SequenceDataNode => throw new ArgumentException($"No mapping node provided for type {value} at line: {node.Start.Line}"),
                        _ => throw new ArgumentException($"Unknown node type {nodeType} provided. Expected mapping node at line: {node.Start.Line}")
                    };
                }

                if (nullable)
                {
                    call = Expression.Condition(
                        Expression.Call(
                            instanceConst,
                            nameof(IsNull),
                            Type.EmptyTypes,
                            nodeParam),
                        Expression.Convert(value.IsValueType ? Expression.Call(instanceConst, nameof(GetNullable), new []{value}) : Expression.Constant(null), typeof(object)),
                        Expression.Convert(call, typeof(object)));
                }
                else
                {
                    call = Expression.Convert(call, typeof(object));
                }

                return Expression.Lambda<ReadDelegate>(
                    call,
                    typeParam,
                    nodeParam,
                    hookCtxParam,
                    contextParam,
                    valueParam).Compile();
            }, (node, this));
        }

        private T? GetNullable<T>() where T : struct
        {
            return null;
        }

        private bool IsNull(DataNode node)
        {
            return node is ValueDataNode valueDataNode && valueDataNode.Value.Trim().ToLower() is "null" or "";
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

        private T[] ReadArraySequenceSealed<T>(
            SequenceDataNode node,
            ReadDelegate elementReader,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null)
        {
            var type = typeof(T);
            var array = new T[node.Sequence.Count];

            for (var i = 0; i < node.Sequence.Count; i++)
            {
                var subNode = node.Sequence[i];
                var result = elementReader(type, subNode, hookCtx, context);
                ReadNullCheck(type, result);
                array[i] = (T) result!;
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
            ValueDataNode node,
            InstantiationDelegate<object> instantiator,
            object? rawValue = null)
            where TValue : ISelfSerialize
        {
            var value = (TValue) (rawValue ?? instantiator());
            value.Deserialize(node.Value);

            return value;
        }

        private TValue ReadWithTypeReader<TValue, TNode>(
            TNode node,
            ITypeReader<TValue, TNode> reader,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null,
            object? value = null)
            where TNode : DataNode
        {
            if (context != null &&
                context.TypeReaders.TryGetValue((typeof(TValue), typeof(TNode)), out var readerUnCast))
            {
                reader = (ITypeReader<TValue, TNode>) readerUnCast;
            }

            return reader.Read(this, node, DependencyCollection, hookCtx, context, value == null ? default : (TValue) value);
        }

        private TValue ReadGenericValue<TValue>(
            ValueDataNode node,
            InstantiationDelegate<object> instantiator,
            DataDefinition? definition,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null,
            object? instance = null)
        {
            var type = typeof(TValue);

            if (context != null &&
                context.TypeReaders.TryGetValue((typeof(TValue), typeof(ValueDataNode)), out var readerUnCast))
            {
                var reader = (ITypeReader<TValue, ValueDataNode>) readerUnCast;
                return reader.Read(this, node, DependencyCollection, hookCtx, context, instance == null ? default : (TValue)instance);
            }

            if (definition == null)
            {
                throw new ArgumentException($"No data definition found for type {type} with node type {node.GetType()} when reading");
            }

            instance ??= instantiator();

            if (node.Value != string.Empty)
            {
                throw new ArgumentException($"No mapping node provided for type {type} at line: {node.Start.Line}");
            }

            RunAfterHook(instance, hookCtx);

            return (TValue) instance;
        }

        private TValue ReadGenericMapping<TValue>(
            MappingDataNode node,
            InstantiationDelegate<object> instantiator,
            DataDefinition? definition,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null,
            object? instance = null)
        {
            var type = typeof(TValue);
            instance ??= instantiator();

            if (context != null &&
                context.TypeReaders.TryGetValue((type, typeof(MappingDataNode)), out var readerUnCast))
            {
                var reader = (ITypeReader<TValue, MappingDataNode>) readerUnCast;
                return reader.Read(this, node, DependencyCollection, hookCtx, context, (TValue?) instance);
            }

            if (definition == null)
            {
                throw new ArgumentException($"No data definition found for type {type} with node type {node.GetType()} when reading");
            }

            var result = (TValue)definition.Populate(instance, node, this, context, hookCtx)!;

            RunAfterHook(result, hookCtx);

            return result;
        }

        public object? ReadWithTypeSerializer(Type type, Type serializer, DataNode node, ISerializationContext? context = null,
            bool skipHook = false, object? value = null)
        {
            return ReadWithTypeSerializer(
                type,
                serializer,
                node,
                SerializationHookContext.ForSkipHooks(skipHook),
                context,
                value);
        }

        public object? ReadWithTypeSerializer(
            Type type,
            Type serializer,
            DataNode node,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null,
            object? value = null)
        {
            return ReadWithSerializerRaw(type, node, serializer, hookCtx, context, value);
        }
    }
}
