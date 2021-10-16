using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using Robust.Shared.Serialization.Manager.Definition;
using Robust.Shared.Serialization.Manager.Result;
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
        private delegate DeserializationResult ReadDelegate(
            Type type,
            DataNode node,
            ISerializationContext? context = null,
            bool skipHook = false);

        private readonly ConcurrentDictionary<(Type value, Type node), ReadDelegate> _readers = new();

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

                MethodCallExpression call;

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
                                Expression.Convert(nodeParam, typeof(ValueDataNode)));
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

                    if (value.IsValueType)
                    {
                        call = Expression.Call(
                            instanceConst,
                            nameof(ReadSelfSerializeNullableStruct),
                            new[] { value },
                            Expression.Convert(nodeParam, typeof(ValueDataNode)),
                            instantiatorConst);
                    }
                    else
                    {
                        call = Expression.Call(
                            instanceConst,
                            nameof(ReadSelfSerialize),
                            new[] { value },
                            Expression.Convert(nodeParam, typeof(ValueDataNode)),
                            instantiatorConst);
                    }
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
                            skipHookParam),
                        _ => Expression.Call(
                            instanceConst,
                            nameof(ReadWithTypeReader),
                            new[] { value, nodeType },
                            Expression.Convert(nodeParam, nodeType),
                            readerConst,
                            contextParam,
                            skipHookParam)
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
                            skipHookParam),
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
                            skipHookParam),
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
                            skipHookParam),
                        SequenceDataNode => throw new ArgumentException($"No mapping node provided for type {value} at line: {node.Start.Line}"),
                        _ => throw new ArgumentException($"Unknown node type {nodeType} provided. Expected mapping node at line: {node.Start.Line}")
                    };
                }

                return Expression.Lambda<ReadDelegate>(
                    call,
                    typeParam,
                    nodeParam,
                    contextParam,
                    skipHookParam).Compile();
            }, (node, this));
        }

        private DeserializationResult ReadArrayValue<T>(ValueDataNode value)
        {
            if (value.Value == "null")
            {
                return new DeserializedValue<T[]?>(null);
            }

            throw new InvalidNodeTypeException("Cannot read an array from a value data node that is not null.");
        }

        private DeserializationResult ReadArraySequence<T>(
            SequenceDataNode node,
            ISerializationContext? context = null,
            bool skipHook = false)
        {
            var type = typeof(T);
            var array = new T[node.Sequence.Count];
            var results = new DeserializationResult[node.Sequence.Count];

            for (var i = 0; i < node.Sequence.Count; i++)
            {
                var subNode = node.Sequence[i];
                var result = Read(type, subNode, context, skipHook);

                results[i] = result;
                array[i] = (T) result.RawValue!;
            }

            return new DeserializedArray(array, results);
        }

        private DeserializationResult ReadArraySequenceSealed<T>(
            SequenceDataNode node,
            ReadDelegate elementReader,
            ISerializationContext? context = null,
            bool skipHook = false)
        {
            var type = typeof(T);
            var array = new T[node.Sequence.Count];
            var results = new DeserializationResult[node.Sequence.Count];

            for (var i = 0; i < node.Sequence.Count; i++)
            {
                var subNode = node.Sequence[i];
                var result = elementReader(type, subNode, context, skipHook);

                results[i] = result;
                array[i] = (T) result.RawValue!;
            }

            return new DeserializedArray(array, results);
        }

        private DeserializationResult ReadEnumNullable<TEnum>(ValueDataNode node) where TEnum : struct
        {
            if (node.Value == "null")
            {
                return new DeserializedValue<TEnum?>(null);
            }

            var value = Enum.Parse<TEnum>(node.Value, true);
            return new DeserializedValue<TEnum?>(value);
        }

        private DeserializationResult ReadEnumValue<TEnum>(ValueDataNode node) where TEnum : struct
        {
            var value = Enum.Parse<TEnum>(node.Value, true);
            return new DeserializedValue<TEnum>(value);
        }

        private DeserializationResult ReadEnumSequence<TEnum>(SequenceDataNode node) where TEnum : struct
        {
            var value = Enum.Parse<TEnum>(string.Join(", ", node.Sequence), true);
            return new DeserializedValue<TEnum>(value);
        }

        private DeserializationResult ReadSelfSerialize<TValue>(
            ValueDataNode node,
            InstantiationDelegate<object> instantiator)
            where TValue : ISelfSerialize
        {
            if (node.Value == "null")
            {
                return new DeserializedValue<TValue?>(default);
            }

            var value = (TValue) instantiator();
            value.Deserialize(node.Value);

            return new DeserializedValue<TValue?>(value);
        }

        private DeserializationResult ReadSelfSerializeNullableStruct<TValue>(
            ValueDataNode node,
            InstantiationDelegate<object> instantiator)
            where TValue : struct, ISelfSerialize
        {
            if (node.Value == "null")
            {
                return new DeserializedValue<TValue?>(null);
            }

            var value = (TValue) instantiator();
            value.Deserialize(node.Value);

            return new DeserializedValue<TValue?>(value);
        }

        private DeserializationResult ReadWithTypeReaderNullable<TValue>(
            ValueDataNode node,
            ITypeReader<TValue, ValueDataNode> reader,
            ISerializationContext? context = null,
            bool skipHook = false)
        {
            if (node.Value == "null")
            {
                return new DeserializedValue<TValue?>(default);
            }

            return ReadWithTypeReader(node, reader, context, skipHook);
        }

        private DeserializationResult ReadWithTypeReaderNullableStruct<TValue>(
            ValueDataNode node,
            ITypeReader<TValue, ValueDataNode> reader,
            ISerializationContext? context = null,
            bool skipHook = false)
            where TValue : struct
        {
            if (node.Value == "null")
            {
                return new DeserializedValue<TValue?>(null);
            }

            return ReadWithTypeReader(node, reader, context, skipHook);
        }

        private DeserializationResult ReadWithTypeReader<TValue, TNode>(
            TNode node,
            ITypeReader<TValue, TNode> reader,
            ISerializationContext? context = null,
            bool skipHook = false)
            where TNode : DataNode
        {
            if (context != null &&
                context.TypeReaders.TryGetValue((typeof(TValue), typeof(TNode)), out var readerUnCast))
            {
                reader = (ITypeReader<TValue, TNode>) readerUnCast;
            }

            return reader.Read(this, node, DependencyCollection, skipHook, context);
        }

        private DeserializationResult ReadGenericNullable<TValue>(
            ValueDataNode node,
            InstantiationDelegate<object> instantiator,
            DataDefinition? definition,
            bool populate,
            bool hooks,
            ISerializationContext? context = null,
            bool skipHook = false)
        {
            if (node.Value == "null")
            {
                return new DeserializedValue<TValue?>(default);
            }

            return ReadGenericValue<TValue?>(node, instantiator, definition, populate, hooks, context, skipHook);
        }

        private DeserializationResult ReadGenericValue<TValue>(
            ValueDataNode node,
            InstantiationDelegate<object> instantiator,
            DataDefinition? definition,
            bool populate,
            bool hooks,
            ISerializationContext? context = null,
            bool skipHook = false)
        {
            var type = typeof(TValue);

            if (context != null &&
                context.TypeReaders.TryGetValue((typeof(TValue), typeof(ValueDataNode)), out var readerUnCast))
            {
                var reader = (ITypeReader<TValue, ValueDataNode>) readerUnCast;
                return reader.Read(this, node, DependencyCollection, skipHook, context);
            }

            if (definition == null)
            {
                throw new ArgumentException($"No data definition found for type {type} with node type {node.GetType()} when reading");
            }

            var instance = instantiator();

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

            var result = definition.Populate(instance, mapping, this, context, skipHook);

            if (!skipHook && hooks)
            {
                ((ISerializationHooks) result.RawValue!).AfterDeserialization();
            }

            return result;
        }

        private DeserializationResult ReadGenericMapping<TValue>(
            MappingDataNode node,
            InstantiationDelegate<object> instantiator,
            DataDefinition? definition,
            bool populate,
            bool hooks,
            ISerializationContext? context = null,
            bool skipHook = false)
        {
            var type = typeof(TValue);
            var instance = instantiator();

            if (context != null &&
                context.TypeReaders.TryGetValue((type, typeof(MappingDataNode)), out var readerUnCast))
            {
                var reader = (ITypeReader<TValue, MappingDataNode>) readerUnCast;
                return reader.Read(this, node, DependencyCollection, skipHook, context);
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

        public DeserializationResult Read(Type type, DataNode node, ISerializationContext? context = null, bool skipHook = false)
        {
            return GetOrCreateReader(type, node)(type, node, context, skipHook);
        }

        public object? ReadValue(Type type, DataNode node, ISerializationContext? context = null, bool skipHook = false)
        {
            return Read(type, node, context, skipHook).RawValue;
        }

        public T? ReadValueCast<T>(Type type, DataNode node, ISerializationContext? context = null, bool skipHook = false)
        {
            var value = Read(type, node, context, skipHook);

            if (value.RawValue == null)
            {
                return default;
            }

            return (T?) value.RawValue;
        }

        public T? ReadValue<T>(DataNode node, ISerializationContext? context = null, bool skipHook = false)
        {
            return ReadValueCast<T>(typeof(T), node, context, skipHook);
        }

        public DeserializationResult ReadWithTypeSerializer(Type value, Type serializer, DataNode node, ISerializationContext? context = null,
            bool skipHook = false)
        {
            return ReadWithSerializerRaw(value, node, serializer, context, skipHook);
        }
    }
}
