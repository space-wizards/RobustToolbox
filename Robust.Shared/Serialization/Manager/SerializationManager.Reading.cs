using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
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
        private delegate object? ReadBoxingDelegate(
            DataNode node,
            ISerializationContext? context = null,
            bool skipHook = false,
            object? value = null);

        private delegate T ReadGenericDelegate<T>(
            DataNode node,
            ISerializationContext? context = null,
            bool skipHook = false,
            T? value = default);

        private readonly ConcurrentDictionary<Type, ReadBoxingDelegate> _readBoxingDelegates = new();
        private readonly ConcurrentDictionary<(Type baseType, Type actualType, Type node), object> _readGenericBaseDelegates = new();
        private readonly ConcurrentDictionary<(Type value, Type node), object> _readGenericDelegates = new();

        public T Read<T>(DataNode node, ISerializationContext? context = null, bool skipHook = false, T? value = default) //todo paul this default should be null
        {
            // we ignore the null check here due to the method only returning a nullable if the type is actually nullable
            // (at least in theory, still waiting on my dotnet api proposal to get approved)
            return GetOrCreateGenericReadDelegate<T>(node)(node, context, skipHook, value);
        }

        public object? Read(Type type, DataNode node, ISerializationContext? context = null, bool skipHook = false, object? value = null)
        {
            return GetOrCreateBoxingReadDelegate(type)(node, context, skipHook, value);
        }

        private ReadBoxingDelegate GetOrCreateBoxingReadDelegate(Type type)
        {
            return _readBoxingDelegates.GetOrAdd(type, static (type, manager) =>
            {
                var managerConst = Expression.Constant(manager);

                var nodeParam = Expression.Variable(typeof(DataNode));
                var contextParam = Expression.Variable(typeof(ISerializationContext));
                var skipHookParam = Expression.Variable(typeof(bool));
                var valueParam = Expression.Variable(typeof(object));

                var call = Expression.Convert(Expression.Call(
                    managerConst,
                    nameof(Read),
                    new[] { type },
                    nodeParam,
                    contextParam,
                    skipHookParam,
                    Expression.Convert(valueParam, type)), typeof(object));

                return Expression.Lambda<ReadBoxingDelegate>(
                    call,
                    nodeParam,
                    contextParam,
                    skipHookParam,
                    valueParam).Compile();
            }, this);
        }

        private ReadGenericDelegate<T> GetOrCreateGenericReadDelegate<T>(DataNode node)
        {
            static object ValueFactory(Type baseType, Type actualType, Type nodeType, SerializationManager manager)
            {
                var nullable = actualType.IsNullable();
                actualType = actualType.EnsureNotNullableType();

                var managerConst = Expression.Constant(manager);

                var nodeParam = Expression.Parameter(typeof(DataNode), "node");
                //todo paul serializers in the context should also override default serializers for array etc
                var contextParam = Expression.Parameter(typeof(ISerializationContext), "context");
                var skipHookParam = Expression.Parameter(typeof(bool), "skipHook");
                var valueParam = Expression.Parameter(baseType, "value");

                var sameType = baseType == actualType;

                Expression call;
                if (manager._regularSerializerProvider.TryGetTypeNodeSerializer(typeof(ITypeReader<,>), actualType, nodeType, out var reader))
                {
                    var readerType = typeof(ITypeReader<,>).MakeGenericType(actualType, nodeType);
                    var readerConst = Expression.Constant(reader, readerType);
                    var depencencyConst = Expression.Constant(manager.DependencyCollection);

                    call = Expression.Call(readerConst, readerType.GetMethod("Read")!, managerConst,
                        Expression.Convert(nodeParam, nodeType), depencencyConst, skipHookParam, contextParam,
                        sameType ? valueParam : Expression.Convert(valueParam, actualType));
                }
                else if (actualType.IsArray)
                {
                    var elementType = actualType.GetElementType()!;

                    if (nodeType == typeof(ValueDataNode))
                    {
                        call = Expression.Call(managerConst, nameof(ReadArrayValue), new[] { elementType },
                            Expression.Convert(nodeParam, typeof(ValueDataNode)), contextParam, skipHookParam);
                    }
                    else if (nodeType == typeof(SequenceDataNode))
                    {
                        call = Expression.Call(managerConst, nameof(ReadArraySequence), new[] { elementType },
                            Expression.Convert(nodeParam, typeof(SequenceDataNode)), contextParam, skipHookParam);
                    }
                    else
                    {
                        throw new ArgumentException($"Cannot read array from data node type {nodeType}");
                    }
                }
                else if (actualType.IsEnum)
                {
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
                        Expression.Assign(valueParam, ValueSafeCoalesceExpression(valueParam, manager.NewExpressionDefault(actualType))),
                        Expression.Call(valueParam, typeof(ISelfSerialize).GetMethod("Deserialize")!,
                            Expression.Field(Expression.Convert(nodeParam, typeof(ValueDataNode)), "Value")));
                }
                else
                {
                    var definition = manager.GetDefinition(actualType);
                    var definitionConst = Expression.Constant(definition, typeof(DataDefinition<>).MakeGenericType(actualType));

                    var hooksConst = Expression.Constant(actualType.IsAssignableTo(typeof(ISerializationHooks)));

                    if (nodeType == typeof(ValueDataNode))
                    {
                        call = Expression.Call(managerConst, nameof(ReadGenericValue), new[] { actualType },
                            Expression.Convert(nodeParam, typeof(ValueDataNode)), definitionConst, hooksConst,
                            contextParam, skipHookParam, sameType ? valueParam : Expression.Convert(valueParam, actualType));
                    }
                    else if (nodeType == typeof(MappingDataNode))
                    {
                        call = Expression.Call(managerConst, nameof(ReadGenericMapping), new[] { actualType },
                            Expression.Convert(nodeParam, typeof(MappingDataNode)), definitionConst, hooksConst,
                            contextParam, skipHookParam, sameType ? valueParam : Expression.Convert(valueParam, actualType));
                    }
                    else
                    {
                        throw new ArgumentException($"No mapping or value node provided for type {actualType}.");
                    }

                    call = Expression.Block(
                        Expression.Assign(valueParam, ValueSafeCoalesceExpression(valueParam, manager.NewExpressionDefault(actualType))),
                        call);
                }

                // early-out null
                var returnValue = Expression.Variable(actualType);
                call = Expression.Block(new[] { returnValue },
                    Expression.Condition(
                    Expression.Call(managerConst, nameof(IsNull), Type.EmptyTypes, nodeParam), nullable
                        ? Expression.Block(typeof(void),
                            Expression.Assign(returnValue, GetNullExpression(managerConst, actualType)))
                        : ThrowExpression<InvalidOperationException>(),
                    Expression.Block(typeof(void),
                        Expression.Assign(returnValue, call))),
                    returnValue);

                // check for customtypeserializer before anything
                var serializerType = typeof(ITypeReader<,>).MakeGenericType(actualType, nodeType);
                var dependencyConst = Expression.Constant(manager.DependencyCollection);
                var serializerVar = Expression.Variable(serializerType);
                call = Expression.Block(new[] { serializerVar },
                    Expression.Condition(
                        Expression.AndAlso(
                            Expression.ReferenceNotEqual(contextParam,
                                Expression.Constant(null, typeof(ISerializationContext))),
                            Expression.Call(Expression.Property(contextParam, "SerializerProvider"),
                                "TryGetTypeNodeSerializer", new[] { serializerType, actualType, nodeType }, serializerVar)),
                        Expression.Call(serializerVar, serializerType.GetMethod("Read")!, managerConst,
                            Expression.Convert(nodeParam, nodeType), dependencyConst, skipHookParam, contextParam,
                            Expression.Convert(valueParam, actualType)),
                        call));

                if (!nullable && !actualType.IsValueType)
                {
                    // check that value isn't null
                    var finalValue = Expression.Variable(typeof(T));
                    call = Expression.Block(new[] { finalValue }, Expression.Assign(finalValue, call),
                        Expression.IfThen(Expression.Equal(finalValue, GetNullExpression(managerConst, actualType)),
                            ThrowExpression<InvalidOperationException>()), finalValue);
                }

                return Expression.Lambda<ReadGenericDelegate<T>>(call, nodeParam,
                    contextParam, skipHookParam, valueParam).Compile();
            }

            if (node.Tag?.StartsWith("!type:") ?? false)
            {
                var type = ResolveConcreteType(typeof(T), node.Tag.Substring(6));
                if (type.IsInterface || type.IsAbstract)
                {
                    throw new ArgumentException($"Interface or abstract type used for !type node. Type: {type}");
                }

                return (ReadGenericDelegate<T>)_readGenericBaseDelegates.GetOrAdd(
                    (typeof(T), type, node.GetType()!),
                    static (tuple, manager) => ValueFactory(tuple.baseType, tuple.actualType, tuple.node, manager),
                    this);
            }

            return (ReadGenericDelegate<T>)_readGenericDelegates.GetOrAdd((typeof(T), node.GetType()!),
                static (tuple, manager) => ValueFactory(tuple.value, tuple.value, tuple.node, manager), this);
        }

        private T[] ReadArrayValue<T>(
            ValueDataNode value,
            ISerializationContext? context = null,
            bool skipHook = false)
        {
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

        private TEnum ReadEnumValue<TEnum>(ValueDataNode node) where TEnum : struct
        {
            return Enum.Parse<TEnum>(node.Value, true);
        }

        private TEnum ReadEnumSequence<TEnum>(SequenceDataNode node) where TEnum : struct
        {
            return Enum.Parse<TEnum>(string.Join(", ", node.Sequence), true);
        }

        private TValue ReadGenericValue<TValue>(
            ValueDataNode node,
            DataDefinition<TValue>? definition,
            bool hooks,
            ISerializationContext? context,
            bool skipHook,
            TValue instance)
            where TValue : notnull
        {
            var type = typeof(TValue);

            if (definition == null)
            {
                throw new ArgumentException($"No data definition found for type {type} with node type {node.GetType()} when reading");
            }

            if (node.Value != string.Empty)
            {
                throw new ArgumentException($"No mapping node provided for type {type} at line: {node.Start.Line}");
            }

            if (!skipHook && hooks)
            {
                ((ISerializationHooks) instance).AfterDeserialization();
            }

            return instance;
        }

        private TValue ReadGenericMapping<TValue>(
            MappingDataNode node,
            DataDefinition<TValue>? definition,
            bool hooks,
            ISerializationContext? context,
            bool skipHook,
            TValue instance)
            where TValue : notnull
        {
            if (definition == null)
            {
                throw new ArgumentException($"No data definition found for type {typeof(TValue)} with node type {node.GetType()} when reading");
            }

            definition.Populate(ref instance, node, context, skipHook);

            if (!skipHook && hooks)
            {
                ((ISerializationHooks) instance).AfterDeserialization();
            }

            return instance;
        }
    }
}
