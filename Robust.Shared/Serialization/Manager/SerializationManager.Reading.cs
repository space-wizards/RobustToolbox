using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq.Expressions;
using Robust.Shared.Serialization.Manager.Definition;
using Robust.Shared.Serialization.Manager.Exceptions;
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
            bool skipHook = false);

        private delegate T ReadGenericDelegate<T>(
            DataNode node,
            ISerializationContext? context = null,
            bool skipHook = false,
            ISerializationManager.InstantiationDelegate<T>? instanceProvider = null);

        private readonly ConcurrentDictionary<Type, ReadBoxingDelegate> _readBoxingDelegates = new();
        private readonly ConcurrentDictionary<(Type baseType, Type actualType, Type node), object> _readGenericBaseDelegates = new();
        private readonly ConcurrentDictionary<(Type value, Type node), object> _readGenericDelegates = new();

        public T Read<T>(DataNode node, ISerializationContext? context = null, bool skipHook = false, ISerializationManager.InstantiationDelegate<T>? instanceProvider = null)
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
                    return GetOrCreateInstantiator<T>(false, type)();

                return ((ReadGenericDelegate<T>)_readGenericBaseDelegates.GetOrAdd(
                    (typeof(T), type, node.GetType()!),
                    static (tuple, manager) => ReadDelegateValueFactory(tuple.baseType, tuple.actualType, tuple.node, manager),
                    this))(node, context, skipHook, instanceProvider);
            }

            return ((ReadGenericDelegate<T>)_readGenericDelegates.GetOrAdd((typeof(T), node.GetType()!),
                static (tuple, manager) => ReadDelegateValueFactory(tuple.value, tuple.value, tuple.node, manager), this))(node, context, skipHook, instanceProvider);
        }

        public T Read<T, TNode>(ITypeReader<T, TNode> reader, TNode node, ISerializationContext? context = null,
            bool skipHook = false, ISerializationManager.InstantiationDelegate<T>? instanceProvider = null)
            where TNode : DataNode
        {
            return reader.Read(this, node, DependencyCollection, skipHook, context, instanceProvider);
        }

        public T Read<T, TNode, TReader>(TNode node, ISerializationContext? context = null,
            bool skipHook = false, ISerializationManager.InstantiationDelegate<T>? instanceProvider = null) where TNode : DataNode
            where TReader : ITypeReader<T, TNode>
        {
            return Read(GetOrCreateCustomTypeSerializer<TReader>(), node, context, skipHook,
                instanceProvider);
        }

        public object? Read(Type type, DataNode node, ISerializationContext? context = null, bool skipHook = false)
        {
            return GetOrCreateBoxingReadDelegate(type)(node, context, skipHook);
        }

        private ReadBoxingDelegate GetOrCreateBoxingReadDelegate(Type type)
        {
            return _readBoxingDelegates.GetOrAdd(type, static (type, manager) =>
            {
                var managerConst = Expression.Constant(manager);

                var nodeParam = Expression.Variable(typeof(DataNode));
                var contextParam = Expression.Variable(typeof(ISerializationContext));
                var skipHookParam = Expression.Variable(typeof(bool));

                var call = Expression.Convert(Expression.Call(
                    managerConst,
                    nameof(Read),
                    new[] { type },
                    nodeParam,
                    contextParam,
                    skipHookParam,
                    Expression.Constant(null, typeof(ISerializationManager.InstantiationDelegate<>).MakeGenericType(type))), typeof(object));

                return Expression.Lambda<ReadBoxingDelegate>(
                    call,
                    nodeParam,
                    contextParam,
                    skipHookParam).Compile();
            }, this);
        }

        private static object ReadDelegateValueFactory(Type baseType, Type actualType, Type nodeType, SerializationManager manager)
        {
            var nullable = actualType.IsNullable();
            actualType = actualType.EnsureNotNullableType();

            var managerConst = Expression.Constant(manager);

            var nodeParam = Expression.Parameter(typeof(DataNode), "node");
            var contextParam = Expression.Parameter(typeof(ISerializationContext), "context");
            var skipHookParam = Expression.Parameter(typeof(bool), "skipHook");
            var instantiatorParam = Expression.Parameter(typeof(ISerializationManager.InstantiationDelegate<>).MakeGenericType(baseType), "instanceProvider");

            var instantiatorVariable =
                Expression.Variable(typeof(ISerializationManager.InstantiationDelegate<>).MakeGenericType(actualType));

            Expression BaseInstantiatorToActual()
            {
                return nullable && baseType.IsValueType
                    ? Expression.Call(
                        managerConst,
                        nameof(UnwrapInstantiationDelegate),
                        new[] { baseType.EnsureNotNullableType() },
                        instantiatorParam)
                    : Expression.Convert(instantiatorParam, typeof(ISerializationManager.InstantiationDelegate<>).MakeGenericType(actualType));
            }

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
                    contextParam,
                    skipHookParam,
                    BaseInstantiatorToActual());
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
                var hooksConst = Expression.Constant(actualType.IsAssignableTo(typeof(ISerializationHooks)));

                if (nodeType == typeof(ValueDataNode))
                {
                    call = Expression.Call(managerConst, nameof(ReadGenericValue), new[] { actualType },
                        Expression.Convert(nodeParam, typeof(ValueDataNode)), hooksConst, skipHookParam,
                        instantiatorVariable);
                }
                else if (nodeType == typeof(MappingDataNode))
                {
                    var definition = manager.GetDefinition(actualType);
                    var definitionConst = Expression.Constant(definition, typeof(DataDefinition<>).MakeGenericType(actualType));

                    call = Expression.Call(managerConst, nameof(ReadGenericMapping), new[] { actualType },
                        Expression.Convert(nodeParam, typeof(MappingDataNode)), definitionConst, hooksConst,
                        contextParam, skipHookParam,
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

            //wrap our valuetype in nullable<T> if we are nullable so we can assign it to returnValue
            call = WrapNullableIfNeededExpression(call, nullable, actualType);

            // early-out null
            var returnValue = Expression.Variable(nullable ? actualType.EnsureNullableType() : actualType);
            call = Expression.Block(new[] { returnValue },
                Expression.IfThenElse(
                Expression.Call(managerConst, nameof(IsNull), Type.EmptyTypes, nodeParam),
                nullable
                    ? Expression.Block(typeof(void), Expression.Assign(returnValue, GetNullExpression(managerConst, actualType)))
                    : ExpressionUtils.ThrowExpression<NullNotAllowedException>(),
                Expression.Block(typeof(void),
                    Expression.Assign(returnValue, call))),
                returnValue);

            // check for customtypeserializer before anything
            var serializerType = typeof(ITypeReader<,>).MakeGenericType(actualType, nodeType);
            var serializerVar = Expression.Variable(serializerType);
            call = Expression.Block(new[] { serializerVar },
                Expression.Condition(
                    Expression.AndAlso(
                        Expression.ReferenceNotEqual(contextParam,
                            Expression.Constant(null, typeof(ISerializationContext))),
                        Expression.Call(Expression.Property(contextParam, "SerializerProvider"),
                            "TryGetTypeNodeSerializer", new[] { serializerType, actualType, nodeType }, serializerVar)),
                    WrapNullableIfNeededExpression(
                        Expression.Call(
                            managerConst,
                            nameof(Read),
                            new []{actualType, nodeType},
                            serializerVar,
                            Expression.Convert(nodeParam, nodeType),
                            contextParam,
                            skipHookParam,
                            BaseInstantiatorToActual()), nullable, actualType),
                    call));

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

            return Expression.Lambda(typeof(ReadGenericDelegate<>).MakeGenericType(baseType), call, nodeParam,
                contextParam, skipHookParam, instantiatorParam).Compile();
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

        private TValue ReadSelfSerialize<TValue>(
            ISerializationManager.InstantiationDelegate<TValue> instanceProvider, ValueDataNode node) where TValue : ISelfSerialize
        {
            var val = instanceProvider();
            val.Deserialize(node.Value);
            return val;
        }

        private TValue ReadGenericValue<TValue>(
            ValueDataNode node,
            bool hooks,
            bool skipHook,
            ISerializationManager.InstantiationDelegate<TValue> instanceProvider)
            where TValue : notnull
        {
            var type = typeof(TValue);
            var instance = instanceProvider();

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
            ISerializationManager.InstantiationDelegate<TValue> instanceProvider)
            where TValue : notnull
        {
            if (definition == null)
            {
                throw new ArgumentException($"No data definition found for type {typeof(TValue)} with node type {node.GetType()} when reading");
            }

            var instance = instanceProvider();

            definition.Populate(ref instance, node, context, skipHook);

            if (!skipHook && hooks)
            {
                ((ISerializationHooks) instance).AfterDeserialization();
            }

            return instance;
        }
    }
}
