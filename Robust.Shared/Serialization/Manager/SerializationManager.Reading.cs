using System;
using System.Collections.Concurrent;
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
            ISerializationContext? context = null,
            bool skipHook = false,
            object? value = null);

        private readonly ConcurrentDictionary<(Type value, Type node), ReadDelegate> _readers = new();

        public T Read<T>(DataNode node, ISerializationContext? context = null, bool skipHook = false)
        {
            return (T)Read(typeof(T), node, context, skipHook)!;
        }

        public object? Read(Type type, DataNode node, ISerializationContext? context = null, bool skipHook = false)
        {
            var val = GetOrCreateReader(type, node)(type, node, context, skipHook);
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
                value = ResolveConcreteType(value, typeString);
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
                var contextParam = Expression.Parameter(typeof(ISerializationContext), "context");
                var skipHookParam = Expression.Parameter(typeof(bool), "skipHook");
                var valueParam = Expression.Parameter(typeof(object), "value");

                MethodCallExpression call;

                //todo paul for now, arrays ignore value. maybe change this?
                if (value.IsArray)
                {
                    var elementType = value.GetElementType()!;

                    switch (node)
                    {
                        case ValueDataNode when nullable:
                            call = Expression.Call(
                                instanceConst,
                                nameof(ReadArrayValue),
                                new[] { elementType },
                                Expression.Convert(nodeParam, typeof(ValueDataNode)),
                                contextParam,
                                skipHookParam);
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
                                    contextParam,
                                    skipHookParam);

                                break;
                            }

                            call = Expression.Call(
                                instanceConst,
                                nameof(ReadArraySequence),
                                new[] { elementType },
                                Expression.Convert(nodeParam, typeof(SequenceDataNode)),
                                contextParam,
                                skipHookParam);
                            break;
                        default:
                            throw new ArgumentException($"Cannot read array from data node type {nodeType}");
                    }
                }
                else if (value.IsEnum)
                {
                    call = node switch
                    {
                        ValueDataNode when nullable => Expression.Call(
                            instanceConst,
                            nameof(ReadEnumNullable),
                            new[] { value },
                            Expression.Convert(nodeParam, typeof(ValueDataNode))),
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

                    var instantiator = instance.GetOrCreateInstantiator(value) ?? throw new NullReferenceException($"No instantiator could be made for type {value}");
                    var instantiatorConst = Expression.Constant(instantiator);

                    call = value.IsValueType switch
                    {
                        true when nullable => call = Expression.Call(
                            instanceConst,
                            nameof(ReadSelfSerializeNullableStruct),
                            new[] {value},
                            Expression.Convert(nodeParam, typeof(ValueDataNode)),
                            instantiatorConst),
                        _ => call = Expression.Call(
                            instanceConst,
                            nameof(ReadSelfSerialize),
                            new[] { value },
                            Expression.Convert(nodeParam, typeof(ValueDataNode)),
                            instantiatorConst, valueParam)
                    };
                }
                else if (instance.TryGetTypeReader(value, nodeType, out var reader))
                {
                    var readerType = typeof(ITypeReader<,>).MakeGenericType(value, nodeType);
                    var readerConst = Expression.Constant(reader, readerType);

                    call = node switch
                    {
                        ValueDataNode when nullable && value.IsValueType => Expression.Call(
                            instanceConst,
                            nameof(ReadWithTypeReaderNullableStruct),
                            new[] { value },
                            Expression.Convert(nodeParam, typeof(ValueDataNode)),
                            readerConst,
                            contextParam,
                            skipHookParam),
                        ValueDataNode when nullable && !value.IsValueType => Expression.Call(
                            instanceConst,
                            nameof(ReadWithTypeReaderNullable),
                            new[] { value },
                            Expression.Convert(nodeParam, typeof(ValueDataNode)),
                            readerConst,
                            contextParam,
                            skipHookParam,
                            valueParam),
                        _ => Expression.Call(
                            instanceConst,
                            nameof(ReadWithTypeReader),
                            new[] { value, nodeType },
                            Expression.Convert(nodeParam, nodeType),
                            readerConst,
                            contextParam,
                            skipHookParam,
                            valueParam)
                    };
                }
                else if (value.IsInterface || value.IsAbstract)
                {
                    throw new ArgumentException($"Unable to create an instance of an interface or abstract type. Type: {value}");
                }
                else
                {
                    var definition = instance.GetDefinition(value);
                    var definitionConst = Expression.Constant(definition, typeof(DataDefinition));

                    var instantiator = instance.GetOrCreateInstantiator(value);
                    var instantiatorConst = Expression.Constant(instantiator);

                    var populateConst = Expression.Constant(value.IsAssignableTo(typeof(IPopulateDefaultValues)));
                    var hooksConst = Expression.Constant(value.IsAssignableTo(typeof(ISerializationHooks)));

                    call = node switch
                    {
                        ValueDataNode when nullable => Expression.Call(
                            instanceConst,
                            nameof(ReadGenericNullable),
                            new[] { value },
                            Expression.Convert(nodeParam, typeof(ValueDataNode)),
                            instantiatorConst,
                            definitionConst,
                            populateConst,
                            hooksConst,
                            contextParam,
                            skipHookParam,
                            valueParam),
                        ValueDataNode => Expression.Call(
                            instanceConst,
                            nameof(ReadGenericValue),
                            new[] { value },
                            Expression.Convert(nodeParam, typeof(ValueDataNode)),
                            instantiatorConst,
                            definitionConst,
                            populateConst,
                            hooksConst,
                            contextParam,
                            skipHookParam,
                            valueParam),
                        MappingDataNode => Expression.Call(
                            instanceConst,
                            nameof(ReadGenericMapping),
                            new[] { value },
                            Expression.Convert(nodeParam, typeof(MappingDataNode)),
                            instantiatorConst,
                            definitionConst,
                            populateConst,
                            hooksConst,
                            contextParam,
                            skipHookParam,
                            valueParam),
                        SequenceDataNode => throw new ArgumentException($"No mapping node provided for type {value} at line: {node.Start.Line}"),
                        _ => throw new ArgumentException($"Unknown node type {nodeType} provided. Expected mapping node at line: {node.Start.Line}")
                    };
                }

                return Expression.Lambda<ReadDelegate>(
                    call,
                    typeParam,
                    nodeParam,
                    contextParam,
                    skipHookParam,
                    valueParam).Compile();
            }, (node, this));
        }

        private T[]? ReadArrayValue<T>(
            ValueDataNode value,
            ISerializationContext? context = null,
            bool skipHook = false)
        {
            if (value.Value == "null")
            {
                return null;
            }

            var array = new T[1];
            array[0] = Read<T>(value, context, skipHook);
            return array;
        }

        private T[] ReadArraySequence<T>(
            SequenceDataNode node,
            ISerializationContext? context = null,
            bool skipHook = false)
        {
            var array = new T[node.Sequence.Count];

            for (var i = 0; i < node.Sequence.Count; i++)
            {
                array[i] = Read<T>(node.Sequence[i], context, skipHook);
            }

            return array;
        }

        private T[] ReadArraySequenceSealed<T>(
            SequenceDataNode node,
            ReadDelegate elementReader,
            ISerializationContext? context = null,
            bool skipHook = false)
        {
            var type = typeof(T);
            var array = new T[node.Sequence.Count];

            for (var i = 0; i < node.Sequence.Count; i++)
            {
                var subNode = node.Sequence[i];
                var result = elementReader(type, subNode, context, skipHook);
                ReadNullCheck(type, result);
                array[i] = (T) result!;
            }

            return array;
        }

        private TEnum? ReadEnumNullable<TEnum>(ValueDataNode node) where TEnum : struct
        {
            if (node.Value == "null")
            {
                return null;
            }

            return ReadEnumValue<TEnum>(node);
        }

        private TEnum ReadEnumValue<TEnum>(ValueDataNode node) where TEnum : struct
        {
            return Enum.Parse<TEnum>(node.Value, true);
        }

        private TEnum ReadEnumSequence<TEnum>(SequenceDataNode node) where TEnum : struct
        {
            return Enum.Parse<TEnum>(string.Join(", ", node.Sequence), true);
        }

        private TValue? ReadSelfSerialize<TValue>(
            ValueDataNode node,
            InstantiationDelegate<object> instantiator, object? rawValue = null)
            where TValue : ISelfSerialize
        {
            if (node.Value == "null")
            {
                return null;
            }

            var value = (TValue) (rawValue ?? instantiator());
            value.Deserialize(node.Value);

            return value;
        }

        private TValue? ReadSelfSerializeNullableStruct<TValue>(
            ValueDataNode node,
            InstantiationDelegate<object> instantiator)
            where TValue : struct, ISelfSerialize
        {
            if (node.Value == "null")
            {
                return null;
            }

            var value = (TValue) instantiator();
            value.Deserialize(node.Value);
            return value;
        }

        private TValue? ReadWithTypeReaderNullable<TValue>(
            ValueDataNode node,
            ITypeReader<TValue, ValueDataNode> reader,
            ISerializationContext? context = null,
            bool skipHook = false,
            object? value = null)
        {
            if (node.Value == "null")
            {
                return null;
            }

            return ReadWithTypeReader(node, reader, context, skipHook, value);
        }

        private TValue? ReadWithTypeReaderNullableStruct<TValue>(
            ValueDataNode node,
            ITypeReader<TValue, ValueDataNode> reader,
            ISerializationContext? context = null,
            bool skipHook = false)
            where TValue : struct
        {
            if (node.Value == "null")
            {
                return null;
            }

            return ReadWithTypeReader(node, reader, context, skipHook);
        }

        private TValue ReadWithTypeReader<TValue, TNode>(
            TNode node,
            ITypeReader<TValue, TNode> reader,
            ISerializationContext? context = null,
            bool skipHook = false,
            object? value = null)
            where TNode : DataNode
        {
            if (context != null &&
                context.TypeReaders.TryGetValue((typeof(TValue), typeof(TNode)), out var readerUnCast))
            {
                reader = (ITypeReader<TValue, TNode>) readerUnCast;
            }

            return reader.Read(this, node, DependencyCollection, skipHook, context, (TValue?) value);
        }

        private TValue? ReadGenericNullable<TValue>(
            ValueDataNode node,
            InstantiationDelegate<object> instantiator,
            DataDefinition? definition,
            bool populate,
            bool hooks,
            ISerializationContext? context = null,
            bool skipHook = false,
            object? value = null)
        {
            if (node.Value == "null")
            {
                return null;
            }

            return ReadGenericValue<TValue>(node, instantiator, definition, populate, hooks, context, skipHook, value);
        }

        private TValue ReadGenericValue<TValue>(
            ValueDataNode node,
            InstantiationDelegate<object> instantiator,
            DataDefinition? definition,
            bool populate,
            bool hooks,
            ISerializationContext? context = null,
            bool skipHook = false,
            object? instance = null)
        {
            var type = typeof(TValue);

            if (context != null &&
                context.TypeReaders.TryGetValue((typeof(TValue), typeof(ValueDataNode)), out var readerUnCast))
            {
                var reader = (ITypeReader<TValue, ValueDataNode>) readerUnCast;
                return reader.Read(this, node, DependencyCollection, skipHook, context, (TValue?) instance);
            }

            if (definition == null)
            {
                throw new ArgumentException($"No data definition found for type {type} with node type {node.GetType()} when reading");
            }

            instance ??= instantiator();

            if (populate)
            {
                ((IPopulateDefaultValues) instance).PopulateDefaultValues();
            }

            if (node.Value != string.Empty)
            {
                throw new ArgumentException($"No mapping node provided for type {type} at line: {node.Start.Line}");
            }

            // If we get an empty ValueDataNode we just use an empty mapping
            var mapping = new MappingDataNode();

            //todo make this just be read
            var result = definition.Populate(instance, mapping, this, context, skipHook);

            if (!skipHook && hooks)
            {
                ((ISerializationHooks) result.RawValue!).AfterDeserialization();
            }

            return result;
        }

        private TValue ReadGenericMapping<TValue>(
            MappingDataNode node,
            InstantiationDelegate<object> instantiator,
            DataDefinition? definition,
            bool populate,
            bool hooks,
            ISerializationContext? context = null,
            bool skipHook = false,
            object? instance = null)
        {
            var type = typeof(TValue);
            instance ??= instantiator();

            if (context != null &&
                context.TypeReaders.TryGetValue((type, typeof(MappingDataNode)), out var readerUnCast))
            {
                var reader = (ITypeReader<TValue, MappingDataNode>) readerUnCast;
                return reader.Read(this, node, DependencyCollection, skipHook, context, (TValue?) instance);
            }

            if (definition == null)
            {
                throw new ArgumentException($"No data definition found for type {type} with node type {node.GetType()} when reading");
            }

            if (populate)
            {
                ((IPopulateDefaultValues) instance).PopulateDefaultValues();
            }

            var result = definition.Populate(instance, node, this, context, skipHook);

            if (!skipHook && hooks)
            {
                ((ISerializationHooks) result.RawValue!).AfterDeserialization();
            }

            return result;
        }

        public object? ReadWithTypeSerializer(Type type, Type serializer, DataNode node, ISerializationContext? context = null,
            bool skipHook = false, object? value = null)
        {
            return ReadWithSerializerRaw(type, node, serializer, context, skipHook, value);
        }
    }
}
